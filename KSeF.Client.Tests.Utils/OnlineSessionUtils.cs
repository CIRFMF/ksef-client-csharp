#nullable enable
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Helpers;
using System.Globalization;
using System.Text;

namespace KSeF.Client.Tests.Utils;

/// <summary>
/// Zawiera metody pomocnicze do obsługi sesji online w systemie KSeF.
/// </summary>
public static class OnlineSessionUtils
{
    private const SystemCode DefaultSystemCode = SystemCode.FA3;
    private const int ProcessingStatusCode = 150;
    private const int DefaultSleepTimeMs = 1000;
    private const int DefaultMaxAttempts = 60;

    /// <summary>
    /// Otwiera nową sesję online w systemie KSeF.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="systemCode">Kod systemowy formularza.</param>
    /// <param name="schemaVersion">Wersja schematu.</param>
    /// <param name="value">Wartość formularza.</param>
    /// <returns>Odpowiedź z informacjami o otwartej sesji online.</returns>
    public static async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(IKSeFClient ksefClient,
        EncryptionData encryptionData,
        string accessToken,
        SystemCode systemCode = DefaultSystemCode)
    {
        OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
          .Create()
          .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: SystemCodeHelper.GetSchemaVersion(systemCode), value: SystemCodeHelper.GetValue(systemCode))
          .WithEncryption(
              encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
              initializationVector: encryptionData.EncryptionInfo.InitializationVector)
          .Build();

        return await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wysyła fakturę w ramach otwartej sesji online.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="nip">NIP podmiotu.</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed wysłaniem (domyślnie: false).</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    public static async Task<SendInvoiceResponse> SendInvoiceAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        string nip,
        string templatePath,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService,
        bool validateInvoice = false)
    {
        string invoiceFilePath = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(invoiceFilePath))
        {
            throw new FileNotFoundException($"Template not found at: {invoiceFilePath}");
        }

        string xml = GetXmlInvoiceFromPath(invoiceFilePath, nip);

        return await SendInvoice(ksefClient, sessionReferenceNumber, accessToken, encryptionData, cryptographyService, xml, validateInvoice).ConfigureAwait(false);
    }

    /// <summary>
    /// Wysyła fakturę w ramach otwartej sesji online. Faktura zawiera dodatkowo podmiot3 będący jednostką podrzędną podmiotu2. 
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="nip">NIP podmiotu.</param>
    /// <param name="subject">identyfikator nabywcy.</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed wysłaniem (domyślnie: false).</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    public static async Task<SendInvoiceResponse> SendInvoiceAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        string nip, string subject,
        string templatePath,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService,
        bool validateInvoice = false)
    {
        string invoiceFilePath = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(invoiceFilePath))
        {
            throw new FileNotFoundException($"Template not found at: {invoiceFilePath}");
        }

        string xml = GetXmlInvoiceFromPath(invoiceFilePath, nip, subject);

        return await SendInvoice(ksefClient, sessionReferenceNumber, accessToken, encryptionData, cryptographyService, xml, validateInvoice).ConfigureAwait(false);
    }


    /// <summary>
    /// Wysyła fakturę w ramach otwartej sesji online. Faktura zawiera dodatkowo podmiot3 będący jednostką podrzędną podmiotu2. 
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="nip">NIP podmiotu.</param>
    /// <param name="subject">identyfikator nabywcy.</param>
    /// <param name="thirdSubject">identyfikator podmiotu trzeciego.</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed wysłaniem (domyślnie: false).</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    public static async Task<SendInvoiceResponse> SendInvoiceAsync(IKSeFClient ksefClient,
       string sessionReferenceNumber,
       string accessToken,
       string nip, string subject, string thirdSubject,
       string templatePath,
       EncryptionData encryptionData,
       ICryptographyService cryptographyService,
       bool validateInvoice = false)
    {
        string invoiceFilePath = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(invoiceFilePath))
        {
            throw new FileNotFoundException($"Template not found at: {invoiceFilePath}");
        }

        string xml = GetXmlInvoiceFromPath(invoiceFilePath, nip, subject, thirdSubject);

        return await SendInvoice(ksefClient, sessionReferenceNumber, accessToken, encryptionData, cryptographyService, xml, validateInvoice).ConfigureAwait(false);
    }

    /// <summary>
    /// Wysyła fakturę w ramach otwartej sesji online. Faktura zawiera dodatkowo podmiot3 będący jednostką podrzędną podmiotu2. 
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="nip">NIP podmiotu.</param>
    /// <param name="subject">identyfikator nabywcy.</param>
    /// <param name="thirdSubject">identyfikator podmiotu trzeciego.</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    private static string GetXmlInvoiceFromPath(string invoiceFilePath, string nip, string subject = "", string thirdSubject = "")
    {
        string xml = File.ReadAllText(invoiceFilePath, Encoding.UTF8);
        xml = xml.Replace("#nip#", nip);
        xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");

        if (!string.IsNullOrEmpty(subject))
        {
            xml = xml.Replace("#nipOdbiorca#", subject);
        }

        if (!string.IsNullOrEmpty(thirdSubject))
        {
            xml = xml.Replace("#idWewnetrzne#", thirdSubject);
        }

        return xml;
    }

    /// <summary>
    /// Wysyła fakturę w ramach otwartej sesji online.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="supplierNip">NIP podmiotu (sprzedawcy) – podmienia <c>#nip#</c>.</param>
    /// <param name="customerNip">NIP nabywcy – wymagany, jeśli szablon zawiera <c>#buyer_nip#</c>.</param>
    /// <param name="buyerReference">Referencja nabywcy (BT-10) – wymagana, jeśli jest <c>#buyer_reference#</c>.</param>
    /// <param name="iban">IBAN rachunku odbiorcy – wymagany, jeśli jest <c>#iban#</c> (bez walidacji).</param>
    /// <param name="templatePath">Ścieżka do pliku szablonu XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    public static async Task<SendInvoiceResponse> SendPefInvoiceAsync(
     IKSeFClient ksefClient,
     string sessionReferenceNumber,
     string accessToken,
     string supplierNip,
     string customerNip,
     string buyerReference,
     string iban,
     string templatePath,
     EncryptionData encryptionData,
     ICryptographyService cryptographyService)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Template not found at: {path}");
        }

        string xml = File.ReadAllText(path, Encoding.UTF8);

        static string NormalizeIban(string v)
            => string.IsNullOrWhiteSpace(v) ? v : new string([.. v.Where(ch => !char.IsWhiteSpace(ch))]).ToUpperInvariant();

        // dane wejściowe -> tylko normalizacja

        string ibanNorm = NormalizeIban(iban);

        // tokeny faktycznie obecne w szablonie
        bool needBuyerRef = xml.Contains("#buyer_reference#", StringComparison.Ordinal);
        bool needBuyerNip = xml.Contains("#buyer_nip#", StringComparison.Ordinal);
        bool needIban = xml.Contains("#iban#", StringComparison.Ordinal)
                         || xml.Contains("#iban_plain#", StringComparison.Ordinal)
                         || xml.Contains("#iban_masked#", StringComparison.Ordinal);
        bool needIssue = xml.Contains("#issue_date#", StringComparison.Ordinal);
        bool needDue = xml.Contains("#due_date#", StringComparison.Ordinal);

        // wymagane wartości JEŚLI token jest w szablonie (bez walidacji)
        if (xml.Contains("#nip#", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(supplierNip))
        {
            throw new ArgumentException("Template requires #nip# but 'nip' is null/empty.", nameof(supplierNip));
        }

        if (xml.Contains("#supplier_nip#", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(supplierNip))
        {
            throw new ArgumentException("Template requires #supplier_nip# but 'nip' is null/empty.", nameof(supplierNip));
        }

        if (needBuyerNip && string.IsNullOrWhiteSpace(customerNip))
        {
            throw new ArgumentException("Template requires #buyer_nip# but 'buyerNip' is null/empty.", nameof(customerNip));
        }

        if (needBuyerRef && string.IsNullOrWhiteSpace(buyerReference))
        {
            throw new ArgumentException("Template requires #buyer_reference# but 'buyerReference' is null/empty.", nameof(buyerReference));
        }

        if (needIban && string.IsNullOrWhiteSpace(ibanNorm))
        {
            throw new ArgumentException("Template requires #iban#/#iban_plain#/#iban_masked# but 'iban' is null/empty.", nameof(iban));
        }

        // daty: jeśli są tokeny – ustaw defaulty (Issue=today, Due=Issue+14)
        DateTime issueDate = DateTime.Today;
        DateTime dueDate = issueDate.AddDays(14);
        if (needDue && dueDate < issueDate)
        {
            throw new ArgumentException("DueDate earlier than IssueDate.");
        }

        // maska IBAN (tylko transformata z dostarczonych danych)
        static string MaskIban(string ibanValue)
        {
            if (string.IsNullOrEmpty(ibanValue))
            {
                return ibanValue;
            }
            // zostaw prefix kraju i 2 cyfry kontrolne, resztę zamaskuj, końcówka 4 cyfry jawne
            string head = ibanValue.Length >= 4 ? ibanValue[..4] : ibanValue;
            string tail = ibanValue.Length >= 4 ? ibanValue[^4..] : string.Empty;
            int midLen = Math.Max(0, ibanValue.Length - head.Length - tail.Length);
            return head + new string('*', midLen) + tail;
        }

        // podmiany — tylko na podstawie danych wejściowych
        (string token, string value)[] replacements =
        [
        ("#nip#", supplierNip ?? string.Empty),
        ("#supplier_nip#", supplierNip ?? string.Empty),   // alias
        ("#buyer_nip#", customerNip ?? string.Empty),
        ("#buyer_reference#", buyerReference ?? string.Empty),
        ("#iban#", ibanNorm ?? string.Empty),
        ("#iban_plain#", ibanNorm ?? string.Empty),
        ("#iban_masked#", MaskIban(ibanNorm ?? string.Empty)),
        ("#invoice_number#", Guid.NewGuid().ToString()),
        ("#issue_date#", issueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ("#due_date#", dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ];

        foreach ((string token, string value) in replacements)
        {
            if (xml.Contains(token, StringComparison.Ordinal))
            {
                xml = xml.Replace(token, value);
            }
        }

        // fail-fast: nie wysyłaj jeśli zostały markery (wraz z aliasami IBAN/NIP)
        string[] known =
        [
        "#nip#", "#supplier_nip#", "#invoice_number#", "#buyer_nip#", "#buyer_reference#",
        "#iban#", "#iban_plain#", "#iban_masked#", "#issue_date#", "#due_date#"
    ];
        string[] leftovers = [.. known.Where(t => xml.Contains(t, StringComparison.Ordinal))];
        if (leftovers.Length > 0)
        {
            throw new ArgumentException($"Template contains unreplaced token(s): {string.Join(", ", leftovers)}.");
        }

        return await SendInvoice(ksefClient, sessionReferenceNumber, accessToken, encryptionData, cryptographyService, xml).ConfigureAwait(false);
    }


    private static async Task<SendInvoiceResponse> SendInvoice(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService,
        string xml,
        bool validateInvoice = false)
    {
        if (validateInvoice)
        {
            ValidationResult validationResult = ValidationHelper.ValidateInvoiceBeforeSending(xml).ValidationResult;
            if (!validationResult.IsValid)
            {
                throw new InvoiceValidationException(validationResult.Message);
            }
        }

        using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));
        byte[] invoice = memoryStream.ToArray();

        byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
        FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
        FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        return await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wysyła fakturę w formacie XML w ramach otwartej sesji online do systemu KSeF.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="xml">Treść faktury w formacie XML.</param>
    /// <param name="encryptionData">Dane szyfrowania.</param>
    /// <param name="cryptographyService">Serwis kryptograficzny.</param>
    /// <returns>Odpowiedź z informacjami o wysłanej fakturze.</returns>
    /// <exception cref="ArgumentException">Występuje, gdy przekazany XML jest pusty lub niezdefiniowany.</exception>
    public static async Task<SendInvoiceResponse> SendInvoiceFromXmlAsync(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        string xml,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML cannot be empty.", nameof(xml));
        }

        byte[] invoiceBytes = Encoding.UTF8.GetBytes(xml);

        byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(
            invoiceBytes, encryptionData.CipherKey, encryptionData.CipherIv);

        FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoiceBytes);
        FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        return await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera status sesji online, oczekując na jej gotowość.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="sleepTime">Czas oczekiwania pomiędzy próbami (ms).</param>
    /// <param name="maxAttempts">Maksymalna liczba prób.</param>
    /// <returns>Odpowiedź ze statusem sesji.</returns>
    public static async Task<SessionStatusResponse> GetOnlineSessionStatusAsync(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        int sleepTime = DefaultSleepTimeMs,
        int maxAttempts = DefaultMaxAttempts)
    {
        int attempt = 0;

        SessionStatusResponse statusResponse;
        do
        {
            statusResponse = await ksefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken).ConfigureAwait(false);

            if (attempt >= maxAttempts)
            {
                break;
            }

            attempt++;
            await Task.Delay(sleepTime).ConfigureAwait(false);
        } while (statusResponse.SuccessfulInvoiceCount is null);

        return statusResponse;
    }

    /// <summary>
    /// Pobiera status faktury w ramach sesji, oczekując na zakończenie jej przetwarzania.
    /// </summary>
    /// <param name="kSeFClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="invoiceReferenceNumber">Numer referencyjny faktury.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="sleepTime">Czas oczekiwania pomiędzy próbami (ms).</param>
    /// <param name="maxAttempts">Maksymalna liczba prób.</param>
    /// <returns>Odpowiedź ze statusem faktury w sesji.</returns>
    public static async Task<SessionInvoice> GetSessionInvoiceStatusAsync(
        IKSeFClient kSeFClient,
        string sessionReferenceNumber,
        string invoiceReferenceNumber,
        string accessToken,
        int sleepTime = DefaultSleepTimeMs,
        int maxAttempts = DefaultMaxAttempts)
    {
        SessionInvoice sessionInvoiceStatus = null!;

        for (int i = 0; i < maxAttempts; i++)
        {
            sessionInvoiceStatus = await kSeFClient.GetSessionInvoiceAsync(sessionReferenceNumber, invoiceReferenceNumber, accessToken).ConfigureAwait(false);

            if (sessionInvoiceStatus.Status.Code != ProcessingStatusCode) // Trwa przetwarzanie
            {
                return sessionInvoiceStatus;
            }

            await Task.Delay(sleepTime).ConfigureAwait(false);
        }

        return sessionInvoiceStatus;
    }

    /// <summary>
    /// Zamyka sesję online w systemie KSeF.
    /// </summary>
    /// <param name="kSeFClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    public static async Task CloseOnlineSessionAsync(IKSeFClient kSeFClient, string sessionReferenceNumber, string accessToken)
    {
        await kSeFClient.CloseOnlineSessionAsync(sessionReferenceNumber, accessToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera metadane faktur przesłanych w ramach sesji online.
    /// </summary>
    /// <param name="kSeFClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <returns>Odpowiedź z metadanymi faktur sesji.</returns>
    public static async Task<SessionInvoicesResponse> GetSessionInvoicesMetadataAsync(IKSeFClient kSeFClient, string sessionReferenceNumber, string accessToken)
    {
        SessionInvoicesResponse sessionInvoiceResponse = await kSeFClient.GetSessionInvoicesAsync(sessionReferenceNumber, accessToken).ConfigureAwait(false);
        return sessionInvoiceResponse;
    }

    /// <summary>
    /// Pobiera UPO faktury z sesji na podstawie numeru KSeF.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="ksefInvoiceNumber">Numer KSeF faktury.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <returns>UPO w formacie XML.</returns>
    public static async Task<string> GetSessionInvoiceUpoAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string ksefInvoiceNumber,
        string accessToken)
    {
        string upoResponse = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefInvoiceNumber, accessToken, CancellationToken.None).ConfigureAwait(false);
        return upoResponse;
    }

    /// <summary>
    /// Pobiera zbiorcze UPO sesji online.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF.</param>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
    /// <param name="upoReferenceNumber">Numer referencyjny UPO.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <returns>Zbiorcze UPO w formacie XML.</returns>
    public static async Task<string> GetSessionUpoAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string upoReferenceNumber,
        string accessToken)
    {
        string upoResponse = await ksefClient.GetSessionUpoAsync(sessionReferenceNumber, upoReferenceNumber, accessToken, CancellationToken.None).ConfigureAwait(false);
        return upoResponse;
    }
}