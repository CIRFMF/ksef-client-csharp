using System.Diagnostics;
using System.Text.Json;

internal sealed class Program
{
    private const string ExternalsFolder = "Externals";
    private const string GeneratorFolder = "ksef-pdf-generator";
    private const string AssetsFolder = "assets";
    private const string InvoiceFileName = "invoice.xml";

    private enum DocumentType
    {
        Invoice,
        Upo
    }

    private static async Task Main(string[] args)
    {
        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        if (!TryParseArguments(args, projectDir, out DocumentType documentType, out string? xmlPath, out string? additionalData, out string? errorMessage))
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine();
            PrintUsage();
            return;
        }

        string generatorDir = Path.Combine(projectDir, ExternalsFolder, GeneratorFolder);
        string wrapperPath = Path.Combine(projectDir, "generate-pdf-wrapper.mjs");

        Console.WriteLine($"=== Generator PDF KSeF - {(documentType == DocumentType.Invoice ? "Faktura" : "UPO")} ===");
        Console.WriteLine();

        // Sprawdź czy node_modules istnieją
        if (!Directory.Exists(Path.Combine(generatorDir, "node_modules")))
        {
            Console.WriteLine("Instalowanie zależności npm...");
            await RunCommand("npm", "install", generatorDir).ConfigureAwait(false);
        }

        // Build przy pierwszym uruchomieniu
        if (!Directory.Exists(Path.Combine(generatorDir, "dist")))
        {
            Console.WriteLine("Budowanie ksef-pdf-generator...");
            await RunCommand("npm", "run build", generatorDir).ConfigureAwait(false);
        }

		// Tryb wsadowy
		if (Directory.Exists(xmlPath))
		{
			string[] xmlFiles = Directory.GetFiles(xmlPath!, "*.xml");
			if (xmlFiles.Length == 0)
			{
				Console.WriteLine($"Brak plików XML w folderze: {xmlPath}");
				return;
			}

			Console.WriteLine($"Przetwarzanie {xmlFiles.Length} plików XML z folderu: {xmlPath}");
			await RunBatchAsync(wrapperPath, projectDir, documentType, xmlFiles, additionalData)
				.ConfigureAwait(false);

			return;
		}

		// Tryb pojedynczego pliku
		string outputPdfPath = Path.Combine(
			Path.GetDirectoryName(xmlPath!)!,
			$"{Path.GetFileNameWithoutExtension(xmlPath)}.pdf");

		string docType = GetDocTypeString(documentType);
		string wrapperArgs = $"\"{wrapperPath}\" {docType} \"{xmlPath}\" \"{outputPdfPath}\"";
        
        if (!string.IsNullOrEmpty(additionalData))
        {
            string escapedJson = additionalData.Replace("\"", "\\\"");
            wrapperArgs += $" \"{escapedJson}\"";
        }
        
        await RunCommand("node", wrapperArgs, projectDir).ConfigureAwait(false);
    }

	private static async Task RunBatchAsync(
		string wrapperPath,
		string workingDirectory,
		DocumentType documentType,
		string[] xmlFiles,
		string? additionalData)
	{
		string docType = GetDocTypeString(documentType);

		var tasks = xmlFiles.Select(xmlFile => new
		{
			documentType = docType,
			inputXmlPath = xmlFile,
			outputPdfPath = Path.ChangeExtension(xmlFile, ".pdf"),
			additionalDataJson = additionalData
		});

		string resolvedFileName;
		string resolvedWrapperArg;

		if (OperatingSystem.IsWindows())
		{
			resolvedFileName = "cmd.exe";
			resolvedWrapperArg = $"/c node \"{wrapperPath}\"";
		}
		else
		{
			resolvedFileName = "node";
			resolvedWrapperArg = $"\"{wrapperPath}\"";
		}

		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = resolvedFileName,
			Arguments = resolvedWrapperArg,
			WorkingDirectory = workingDirectory,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};  

		using Process process = Process.Start(processStartInfo)!;

		Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
		Task<string> errorTask = process.StandardError.ReadToEndAsync();

		// Wysyłamy zadania przez stdin, jedno zadanie = jedna linia JSON
		StreamWriter stdin = process.StandardInput;
		try
		{
			foreach (var task in tasks)
				await stdin.WriteLineAsync(JsonSerializer.Serialize(task)).ConfigureAwait(false);
		}
		finally
		{
			await stdin.DisposeAsync().ConfigureAwait(false);
		} // zamknięcie stdin sygnalizuje wrapperowi EOF

		string output = await outputTask.ConfigureAwait(false);
		string error = await errorTask.ConfigureAwait(false);

		await process.WaitForExitAsync().ConfigureAwait(false);

		if (!string.IsNullOrWhiteSpace(output))
			Console.WriteLine(output);

		if (!string.IsNullOrWhiteSpace(error))
			Console.Error.WriteLine(error);

		if (process.ExitCode != 0)
			throw new InvalidOperationException($"Proces node zakończył się błędem (kod: {process.ExitCode})");
	}

	private static bool TryParseArguments(string[] args, string projectDir, out DocumentType documentType, out string? xmlPath, out string? additionalData, out string? errorMessage)
    {
        documentType = DocumentType.Invoice;
        xmlPath = null;
        additionalData = null;
        errorMessage = null;

        switch (args.Length)
        {
            case 0:
                // Brak argumentów - użyj domyślnej faktury
                xmlPath = Path.Combine(projectDir, ExternalsFolder, GeneratorFolder, AssetsFolder, InvoiceFileName);
                return true;

            case 1:
                // Jeden argument - ścieżka do pliku/folderu (faktura)
                return ValidateXmlOrFolderPath(args[0], out xmlPath, out errorMessage);

            case 2:
                // Dwa argumenty - typ dokumentu i ścieżka
                if (!TryParseDocumentType(args[0], out documentType))
                {
                    errorMessage = $"Błąd: Nieprawidłowy typ dokumentu: {args[0]}\nDozwolone wartości: faktura, invoice, upo";
                    return false;
                }
                return ValidateXmlOrFolderPath(args[1], out xmlPath, out errorMessage);

            case >= 3:
                // Trzy lub więcej argumentów - typ, ścieżka i additionalData (może być podzielony przez spacje)
                if (!TryParseDocumentType(args[0], out documentType))
                {
                    errorMessage = $"Błąd: Nieprawidłowy typ dokumentu: {args[0]}\nDozwolone wartości: faktura, invoice, upo";
                    return false;
                }
                if (!ValidateXmlOrFolderPath(args[1], out xmlPath, out errorMessage))
                {
                    return false;
                }
                // Połącz wszystkie pozostałe argumenty (JSON mógł zostać podzielony przez spacje)
                additionalData = string.Join(" ", args.Skip(2));
                return true;

            default:
                errorMessage = "Błąd: Zbyt wiele argumentów.";
                return false;
        }
    }

    private static bool TryParseDocumentType(string typeArg, out DocumentType documentType)
    {
        string normalized = typeArg.ToLowerInvariant();
        
        if (normalized is "faktura" or "invoice")
        {
            documentType = DocumentType.Invoice;
            return true;
        }
        
        if (normalized == "upo")
        {
            documentType = DocumentType.Upo;
            return true;
        }

        documentType = default;
        return false;
    }

    private static bool ValidateXmlOrFolderPath(string inputPath, out string? xmlPath, out string? errorMessage)
    {
        xmlPath = Path.GetFullPath(inputPath);

        if (File.Exists(xmlPath) || Directory.Exists(xmlPath))
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"Błąd: Nie znaleziono zasobu: {xmlPath}";
        return false;
    }

	private static string GetDocTypeString(DocumentType documentType) =>
	    documentType == DocumentType.Invoice ? "invoice" : "upo";

	private static async Task RunCommand(string fileName, string arguments, string workingDirectory)
	{
		string resolvedFileName = fileName;
		string resolvedArguments = arguments;

		if (OperatingSystem.IsWindows())
		{
			resolvedFileName = "cmd.exe";
			resolvedArguments = $"/c {fileName} {arguments}";
		}

		ProcessStartInfo processStartInfo = new()
		{
			FileName = resolvedFileName,
			Arguments = resolvedArguments,
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using Process? process = Process.Start(processStartInfo);
        string output = await process!.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }
        if (!string.IsNullOrWhiteSpace(error) && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Błąd: {error}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Użycie:");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [ścieżkaXml]");
		Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [folder]");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [typ] [ścieżkaXml]");
		Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [typ] [folder]");
		Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [typ] [ścieżkaXml] [additionalDataJson]");
        Console.WriteLine();
        Console.WriteLine("Argumenty:");
        Console.WriteLine("  typ                  Typ dokumentu: faktura, invoice lub upo");
        Console.WriteLine("  ścieżkaXml           Ścieżka do pliku XML");
		Console.WriteLine("  folder               Folder z plikami *.xml (tryb wsadowy)");
		Console.WriteLine("  additionalDataJson   Opcjonalne dane JSON (np. nrKSeF, qrCode)");
        Console.WriteLine();
        Console.WriteLine("Przykłady:");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp");
        Console.WriteLine("    (generuje domyślną fakturę z assets/invoice.xml)");
        Console.WriteLine();
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp C:\\ścieżka\\do\\faktury.xml");
        Console.WriteLine("    (generuje fakturę z podanego pliku)");
        Console.WriteLine();
		Console.WriteLine("  KSeF.Client.Tests.PdfTestApp C:\\faktury");
        Console.WriteLine("    (generuje PDFy dla wszystkich *.xml w folderze)");
		Console.WriteLine();
		Console.WriteLine("  KSeF.Client.Tests.PdfTestApp faktura C:\\ścieżka\\do\\faktury.xml");
        Console.WriteLine("    (generuje fakturę z podanego pliku)");
        Console.WriteLine();
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp upo C:\\ścieżka\\do\\upo.xml");
        Console.WriteLine("    (generuje UPO z podanego pliku)");
        Console.WriteLine();
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp faktura C:\\faktura.xml '{\\\"nrKSeF\\\":\\\"1234567890\\\"}'");
        Console.WriteLine("    (generuje fakturę z dodatkowymi danymi KSeF)");
        Console.WriteLine();
        Console.WriteLine("Uwaga: W PowerShell użyj pojedynczych cudzysłowów dla JSON:");
    }
}
