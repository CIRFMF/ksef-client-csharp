#nullable enable
using KSeF.Client.Api.Builders.Batch;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using System.IO.Compression;
using System.Text;

namespace KSeF.Client.Tests.Utils;

/// <summary>
/// Zawiera metody pomocnicze do obsługi sesji wsadowych w systemie KSeF.
/// </summary>
public static class BatchUtils
{
    private const SystemCode DefaultSystemCode = SystemCode.FA3;
    private const string DefaultSchemaVersion = "1-0E";
    private const string DefaultValue = "FA";
    private const int DefaultSleepTimeMs = 1000;
    private const int DefaultMaxAttempts = 60;
    private const int DefaultPageOffset = 0;
    private const int DefaultPageSize = 10;
    private const long MaxPartSizeBytes = 100L * 1000 * 1000; // 100MB

    /// <summary>
    /// Pobiera metadane faktur przesłanych w ramach sesji wsadowej.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="pageSize">Rozmiar strony wyników.</param>
    /// <returns>Odpowiedź z metadanymi faktur sesji.</returns>
    public static async Task<SessionInvoicesResponse> GetSessionInvoicesAsync(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        int pageSize = DefaultPageSize)
        => await ksefClient.GetSessionInvoicesAsync(sessionReferenceNumber, accessToken, pageSize).ConfigureAwait(false);

    /// <summary>
    /// Pobiera UPO faktury z sesji wsadowej na podstawie numeru KSeF.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="ksefNumber">Numer KSeF faktury.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <returns>UPO w formacie XML.</returns>
    public static async Task<string> GetSessionInvoiceUpoByKsefNumberAsync(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string ksefNumber,
        string accessToken)
        => await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefNumber, accessToken).ConfigureAwait(false);

    /// <summary>
    /// Otwiera nową sesję wsadową w systemie KSeF.
    /// </summary>
    /// <param name="client">Klient KSeF.</param>
    /// <param name="openReq">Żądanie otwarcia sesji wsadowej.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <returns>Odpowiedź z informacjami o otwartej sesji wsadowej.</returns>
    public static async Task<OpenBatchSessionResponse> OpenBatchAsync(
        IKSeFClient client,
        OpenBatchSessionRequest openReq,
        string accessToken)
        => await client.OpenBatchSessionAsync(openReq, accessToken).ConfigureAwait(false);

    /// <summary>
    /// Wysyła części paczki faktur w ramach otwartej sesji wsadowej.
    /// </summary>
    /// <param name="client">Klient KSeF.</param>
    /// <param name="openResp">Odpowiedź z otwarcia sesji wsadowej.</param>
    /// <param name="parts">Kolekcja partów do wysłania.</param>
    public static async Task SendBatchPartsAsync(
        IKSeFClient client,
        OpenBatchSessionResponse openResp,
        ICollection<BatchPartSendingInfo> parts)
        => await client.SendBatchPartsAsync(openResp, parts).ConfigureAwait(false);

    /// <summary>
    /// Zamyka sesję wsadową w systemie KSeF.
    /// </summary>
    /// <param name="client">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    public static async Task CloseBatchAsync(
        IKSeFClient client,
        string sessionReferenceNumber,
        string accessToken)
        => await client.CloseBatchSessionAsync(sessionReferenceNumber, accessToken).ConfigureAwait(false);

    /// <summary>
    /// Buduje ZIP w pamięci z podanych plików i zwraca bajty oraz metadane (przed szyfrowaniem).
    /// </summary>
    /// <param name="files">Kolekcja plików do spakowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <returns>Krotka: bajty ZIP oraz metadane pliku.</returns>
    public static (byte[] ZipBytes, FileMetadata Meta) BuildZip(
        IEnumerable<(string FileName, byte[] Content)> files,
        ICryptographyService cryptographyService)
    {
        using MemoryStream zipStream = new();
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Create, leaveOpen: true);

        foreach ((string fileName, byte[] content) in files)
        {
            ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
#if NETFRAMEWORK
            entryStream.Write(content, 0, content.Length);
#else
            entryStream.Write(content);
#endif
        }

        archive.Dispose();

        byte[] zipBytes = zipStream.ToArray();
        FileMetadata meta = cryptographyService.GetMetaData(zipBytes);

        return (zipBytes, meta);
    }

    /// <summary>
    /// Oblicza optymalną liczbę części paczki na podstawie rozmiaru ZIP, 
    /// tak aby każda część nie przekraczała 100MB.
    /// </summary>
    /// <param name="zipSizeBytes">Rozmiar paczki ZIP w bajtach</param>
    /// <returns>Liczba części (minimum 1)</returns>
    public static int CalculateBatchPartQuantity(long zipSizeBytes)
    {
        if (zipSizeBytes <= MaxPartSizeBytes)
        {
            return 1;
        }

        int partCount = (int)Math.Ceiling((double)zipSizeBytes / MaxPartSizeBytes);

        return partCount;
    }

    /// <summary>
    /// Dzieli bufor na określoną liczbę części o zbliżonym rozmiarze.
    /// </summary>
    /// <param name="input">Bufor wejściowy.</param>
    /// <param name="partCount">Liczba części.</param>
    /// <returns>Lista buforów podzielonych na części.</returns>
    public static List<byte[]> Split(byte[] input, int partCount)
    {
        Guard.ThrowIfNull(input);
        Guard.ThrowIfNegativeOrZero(partCount);

        List<byte[]> result = new(partCount);
        int partSize = (int)Math.Ceiling((double)input.Length / partCount);

        for (int i = 0; i < partCount; i++)
        {
            int start = i * partSize;
            int size = Math.Min(partSize, input.Length - start);
            if (size <= 0)
            {
                break;
            }

            byte[] part = new byte[size];
            Array.Copy(input, start, part, 0, size);
            result.Add(part);
        }

        return result;
    }

    /// <summary>
    /// Szyfruje i pakuje do struktur partów (1..N). 
    /// Gdy <paramref name="partCount"/> == null, automatycznie wylicza optymalną liczbę części na podstawie rozmiaru ZIP (max 100MB na część).
    /// Gdy <paramref name="partCount"/> == 1, nie dzieli <paramref name="zipBytes"/> na części.
    /// </summary>
    /// <param name="zipBytes">Bajty ZIP do podziału i zaszyfrowania.</param>
    /// <param name="encryption">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <param name="partCount">Liczba części (opcjonalnie). Jeśli null, zostanie automatycznie wyliczona.</param>
    /// <returns>Lista zaszyfrowanych partów do wysyłki.</returns>
    public static List<BatchPartSendingInfo> EncryptAndSplit(
        byte[] zipBytes,
        EncryptionData encryption,
        ICryptographyService cryptographyService,
        int? partCount = null)
    {
        Guard.ThrowIfNull(zipBytes);
        Guard.ThrowIfNull(encryption);
        Guard.ThrowIfNull(cryptographyService);

        // Jeśli partCount nie jest podane, wylicz automatycznie
        int actualPartCount = partCount ?? CalculateBatchPartQuantity(zipBytes.Length);

        List<byte[]> rawParts = actualPartCount <= 1
            ? [zipBytes]
            : Split(zipBytes, actualPartCount);

        List<BatchPartSendingInfo> result = new(rawParts.Count);

        for (int i = 0; i < rawParts.Count; i++)
        {
            byte[] encrypted = cryptographyService.EncryptBytesWithAES256(rawParts[i], encryption.CipherKey, encryption.CipherIv);
            FileMetadata meta = cryptographyService.GetMetaData(encrypted);

            result.Add(new BatchPartSendingInfo
            {
                Data = encrypted,
                OrdinalNumber = i + 1,
                Metadata = meta
            });
        }

        return result;
    }

    /// <summary>
    /// Buduje żądanie otwarcia sesji wsadowej z kodem formularza i listą zaszyfrowanych partów.
    /// </summary>
    /// <param name="zipMeta">Metadane pliku ZIP.</param>
    /// <param name="encryption">Dane szyfrowania.</param>
    /// <param name="encryptedParts">Lista zaszyfrowanych partów.</param>
    /// <param name="systemCode">Kod systemowy formularza.</param>
    /// <param name="schemaVersion">Wersja schematu.</param>
    /// <param name="value">Wartość formularza.</param>
    /// <returns>Obiekt żądania otwarcia sesji wsadowej.</returns>
    public static OpenBatchSessionRequest BuildOpenBatchRequest(
        FileMetadata zipMeta,
        EncryptionData encryption,
        IEnumerable<BatchPartSendingInfo> encryptedParts,
        SystemCode systemCode = DefaultSystemCode,
        string schemaVersion = DefaultSchemaVersion,
        string value = DefaultValue)
    {
        IOpenBatchSessionRequestBuilderBatchFile builder = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: schemaVersion, value: value)
            .WithBatchFile(fileSize: zipMeta.FileSize, fileHash: zipMeta.HashSHA);

        foreach (BatchPartSendingInfo p in encryptedParts)
        {
            builder = builder.AddBatchFilePart(
                ordinalNumber: p.OrdinalNumber,
                fileName: $"part_{p.OrdinalNumber}.zip.aes",
                fileSize: p.Metadata.FileSize,
                fileHash: p.Metadata.HashSHA);
        }

        return builder
            .EndBatchFile()
            .WithEncryption(
                encryptedSymmetricKey: encryption.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryption.EncryptionInfo.InitializationVector)
            .Build();
    }

    /// <summary>
    /// Sprawdza status sesji wsadowej aż do przetworzenia lub przekroczenia limitu prób.
    /// </summary>
    /// <param name="client">Klient KSeF.</param>
    /// <param name="sessionRef">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="sleepTime">Czas oczekiwania pomiędzy próbami (ms).</param>
    /// <param name="maxAttempts">Maksymalna liczba prób.</param>
    /// <returns>Odpowiedź ze statusem sesji.</returns>
    public static async Task<SessionStatusResponse> WaitForBatchStatusAsync(
        IKSeFClient client,
        string sessionRef,
        string accessToken,
        int sleepTime = DefaultSleepTimeMs,
        int maxAttempts = DefaultMaxAttempts)
    {
        SessionStatusResponse? last = null;

        try
        {
            return await AsyncPollingUtils.PollAsync(
                action: async () =>
                {
                    last = await client.GetSessionStatusAsync(sessionRef, accessToken).ConfigureAwait(false);
                    return last;
                },
                condition: s => s.Status.Code != 150, // 150 = w trakcie przetwarzania
                delay: TimeSpan.FromMilliseconds(sleepTime),
                maxAttempts: maxAttempts
            ).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Zachowujemy poprzednie zachowanie: zwróć ostatni znany status po przekroczeniu limitu prób
            return last!;
        }
    }

    /// <summary>
    /// Generuje dokumenty XML w pamięci na podstawie szablonu i NIP.
    /// </summary>
    /// <param name="count">Liczba dokumentów do wygenerowania.</param>
    /// <param name="nip">NIP podmiotu.</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="invoiceNumberFactory">Funkcja generująca numer faktury (opcjonalnie).</param>
    /// <returns>Lista krotek: nazwa pliku i zawartość w bajtach.</returns>
    public static List<(string FileName, byte[] Content)> GenerateInvoicesInMemory(
        int count,
        string nip,
        string templatePath,
        Func<string>? invoiceNumberFactory = null)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Template not found at: {path}");
        }

        string template = File.ReadAllText(path, Encoding.UTF8);

        List<(string FileName, byte[] Content)> list = new(count);

        for (int i = 0; i < count; i++)
        {
            string xml = template
                .Replace("#nip#", nip)
                .Replace("#invoice_number#", (invoiceNumberFactory?.Invoke() ?? Guid.NewGuid().ToString()));
            list.Add(($"faktura_{i + 1}.xml", Encoding.UTF8.GetBytes(xml)));
        }

        return list;
    }


    /// <summary>
    /// Rozpakowuje archiwum ZIP ze strumienia i zwraca słownik plików (nazwa -> zawartość).
    /// </summary>
    /// <param name="zipStream">Strumień zawierający archiwum ZIP.</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>Słownik zawierający nazwy plików i ich zawartość jako string.</returns>
    public static async Task<Dictionary<string, string>> UnzipAsync(
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(zipStream);

        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);

        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            using Stream entryStream = entry.Open();
            using StreamReader reader = new(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
#if NETFRAMEWORK
            string content = await reader.ReadToEndAsync().ConfigureAwait(false);
#else
            string content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#endif
            files[entry.Name] = content;
        }

        return files;
    }

    /// <summary>
    /// Rozpakowuje archiwum ZIP z tablicy bajtów i zwraca słownik plików (nazwa -> zawartość).
    /// </summary>
    /// <param name="zipBytes">Tablica bajtów zawierająca archiwum ZIP.</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>Słownik zawierający nazwy plików i ich zawartość jako string.</returns>
    public static async Task<Dictionary<string, string>> UnzipAsync(
        byte[] zipBytes,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(zipBytes);

        using MemoryStream stream = new(zipBytes);
        return await UnzipAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera, deszyfruje i łączy części paczki eksportu w jeden strumień.
    /// </summary>
    /// <param name="parts">Kolekcja części paczki do pobrania i połączenia.</param>
    /// <param name="encryptionData">Dane szyfrowania używane do deszyfrowania części.</param>
    /// <param name="crypto">Serwis kryptograficzny.</param>
    /// <param name="httpClientFactory">Funkcja fabrykująca HttpClient (opcjonalnie, domyślnie tworzy nowy HttpClient).</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>Strumień zawierający odszyfrowane i połączone dane.</returns>
    public static async Task<MemoryStream> DownloadAndDecryptPackagePartsAsync(
        IEnumerable<InvoiceExportPackagePart> parts,
        EncryptionData encryptionData,
        ICryptographyService crypto,
        Func<HttpClient>? httpClientFactory = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(parts);
        Guard.ThrowIfNull(encryptionData);
        Guard.ThrowIfNull(crypto);

        MemoryStream decryptedStream = new();

        try
        {
            foreach (InvoiceExportPackagePart? part in parts.OrderBy(p => p.OrdinalNumber))
            {
                byte[] encryptedBytes = await DownloadPackagePartAsync(part, httpClientFactory, cancellationToken).ConfigureAwait(false);
                byte[] decryptedBytes = crypto.DecryptBytesWithAES256(encryptedBytes, encryptionData.CipherKey, encryptionData.CipherIv);

#if NETFRAMEWORK
                await decryptedStream.WriteAsync(decryptedBytes, 0, decryptedBytes.Length, cancellationToken).ConfigureAwait(false);
#else
                await decryptedStream.WriteAsync(decryptedBytes, cancellationToken).ConfigureAwait(false);
#endif
            }

            decryptedStream.Position = 0;
            return decryptedStream;
        }
        catch
        {
            decryptedStream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Pobiera pojedynczą część paczki eksportu z URL.
    /// </summary>
    /// <param name="part">Część paczki do pobrania.</param>
    /// <param name="httpClientFactory">Funkcja fabrykująca HttpClient (opcjonalnie, domyślnie tworzy nowy HttpClient).</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>Tablica bajtów zawierająca pobraną część.</returns>
    private static async Task<byte[]> DownloadPackagePartAsync(
        InvoiceExportPackagePart part,
        Func<HttpClient>? httpClientFactory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(part.Url))
        {
            throw new InvalidOperationException($"Brak URL dla części paczki {part.OrdinalNumber}.");
        }

        using HttpClient httpClient = httpClientFactory?.Invoke() ?? new HttpClient();
        using HttpRequestMessage request = new(new HttpMethod(part.Method ?? HttpMethod.Get.Method), part.Url);
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

#if NETFRAMEWORK
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#else
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#endif
    }
}