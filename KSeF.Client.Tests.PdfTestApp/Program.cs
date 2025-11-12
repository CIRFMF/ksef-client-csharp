using System.Diagnostics;

internal sealed class Program
{
    private const string ExternalsFolder = "Externals";
    private const string GeneratorFolder = "ksef-pdf-generator";
    private const string ExamplesFolder = "examples";
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
        string outputPdfPath = Path.Combine(projectDir, $"{Path.GetFileNameWithoutExtension(xmlPath)}.pdf");

        Console.WriteLine($"=== Generator PDF KSeF - {(documentType == DocumentType.Invoice ? "Faktura" : "UPO")} ===");
        Console.WriteLine();

        // Sprawdź czy node_modules istnieją
        if (!Directory.Exists(Path.Combine(generatorDir, "node_modules")))
        {
            Console.WriteLine("Instalowanie zależności npm...");
            await RunCommand("npm", "install", generatorDir);
        }

        string docType = documentType == DocumentType.Invoice ? "invoice" : "upo";
        string wrapperArgs = $"\"{wrapperPath}\" {docType} \"{xmlPath}\" \"{outputPdfPath}\"";
        
        if (!string.IsNullOrEmpty(additionalData))
        {
            string escapedJson = additionalData.Replace("\"", "\\\"");
            wrapperArgs += $" \"{escapedJson}\"";
        }
        
        await RunCommand("node", wrapperArgs, projectDir);
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
                xmlPath = Path.Combine(projectDir, ExternalsFolder, GeneratorFolder, ExamplesFolder, InvoiceFileName);
                return true;

            case 1:
                // Jeden argument - ścieżka do pliku (faktura)
                return ValidateXmlPath(args[0], out xmlPath, out errorMessage);

            case 2:
                // Dwa argumenty - typ dokumentu i ścieżka
                if (!TryParseDocumentType(args[0], out documentType))
                {
                    errorMessage = $"Błąd: Nieprawidłowy typ dokumentu: {args[0]}\nDozwolone wartości: faktura, invoice, upo";
                    return false;
                }
                return ValidateXmlPath(args[1], out xmlPath, out errorMessage);

            case >= 3:
                // Trzy lub więcej argumentów - typ, ścieżka i additionalData (może być podzielony przez spacje)
                if (!TryParseDocumentType(args[0], out documentType))
                {
                    errorMessage = $"Błąd: Nieprawidłowy typ dokumentu: {args[0]}\nDozwolone wartości: faktura, invoice, upo";
                    return false;
                }
                if (!ValidateXmlPath(args[1], out xmlPath, out errorMessage))
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

    private static bool ValidateXmlPath(string inputPath, out string? xmlPath, out string? errorMessage)
    {
        xmlPath = Path.GetFullPath(inputPath);
        
        if (File.Exists(xmlPath))
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"Błąd: Nie znaleziono pliku XML: {xmlPath}";
        return false;
    }

    private static async Task RunCommand(string fileName, string arguments, string workingDirectory)
    {
        ProcessStartInfo processStartInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(processStartInfo);
        string output = await process!.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

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
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [typ] [ścieżkaXml]");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp [typ] [ścieżkaXml] [additionalDataJson]");
        Console.WriteLine();
        Console.WriteLine("Argumenty:");
        Console.WriteLine("  typ                  Typ dokumentu: faktura, invoice lub upo");
        Console.WriteLine("  ścieżkaXml           Ścieżka do pliku XML");
        Console.WriteLine("  additionalDataJson   Opcjonalne dane JSON (np. nrKSeF, qrCode)");
        Console.WriteLine();
        Console.WriteLine("Przykłady:");
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp");
        Console.WriteLine("    (generuje domyślną fakturę z examples/invoice.xml)");
        Console.WriteLine();
        Console.WriteLine("  KSeF.Client.Tests.PdfTestApp C:\\ścieżka\\do\\faktury.xml");
        Console.WriteLine("    (generuje fakturę z podanego pliku)");
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
