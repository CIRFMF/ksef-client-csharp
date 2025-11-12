using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Tests.Utils.Upo;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.Upo;

[Collection("UpoScenario")]
public class UpoE2ETests : TestBase
{
    private const int SuccessfulSessionStatusCode = 200;
    private const SystemCode DefaultSystemCode = SystemCode.FA3;
    private const string DefaultSchemaVersion = "1-0E";
    private const string DefaultFormCodeValue = "FA";
    private const int SuccessfulInvoiceCountExpected = 1;
    private const int SessionStatusCodeExpected = 100;

    private string accessToken = string.Empty;
    private string nip { get; }

    public UpoE2ETests()
    {
        nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, nip)
                                          .GetAwaiter()
                                          .GetResult();
        accessToken = authInfo.AccessToken.Token;
    }

    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task UpoRetreivalAsync_FullIntegrationFlow_AllStepsSucceed(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // 1) Otwarcie sesji
        OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(encryptionData, systemCode);
        Assert.NotNull(openSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));
        await Task.Delay(SleepTime);

        // 2) Wysłanie faktury
        SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(openSessionResponse.ReferenceNumber, encryptionData, CryptographyService, invoiceTemplatePath, nip);
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
        await Task.Delay(SleepTime);

        // 3) Status po wysłaniu faktury (oczekujemy 1 sukcesu, brak błędów, brak UPO, kod 100)
        SessionStatusResponse statusAfterSend = await GetSessionStatusUntilInvoiceCountAvailableAsync(openSessionResponse.ReferenceNumber);
        Assert.NotNull(statusAfterSend);
        Assert.NotNull(statusAfterSend.SuccessfulInvoiceCount);
        Assert.Equal(SuccessfulInvoiceCountExpected, statusAfterSend.SuccessfulInvoiceCount);
        Assert.Null(statusAfterSend.FailedInvoiceCount);
        Assert.Null(statusAfterSend.Upo);
        Assert.Equal(SessionStatusCodeExpected, statusAfterSend.Status.Code);
        await Task.Delay(SleepTime);

        // 4) Zamknięcie sesji
        await CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber);
        await Task.Delay(SleepTime);

        // 5) Pobranie faktur sesji (powinna być jedna)
        SessionInvoicesResponse invoices = await GetSessionInvoicesAsync(openSessionResponse.ReferenceNumber);
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);
        Assert.Single(invoices.Invoices);
        string ksefNumber = invoices.Invoices.First().KsefNumber;

        // 6) Status po zamknięciu (kod 200) i numer referencyjny UPO
        SessionStatusResponse statusAfterClose = await GetSessionStatusAsync(openSessionResponse.ReferenceNumber);
        Assert.NotNull(statusAfterClose);
        Assert.Equal(SuccessfulSessionStatusCode, statusAfterClose.Status.Code);
        string upoReferenceNumber = statusAfterClose.Upo.Pages.First().ReferenceNumber;

        // 7) pobranie UPO faktury z URL zawartego w metadanych faktury
        Uri upoDownloadUrl = invoices.Invoices.First().UpoDownloadUrl;
        string invoiceUpoXml = await UpoUtils.GetUpoAsync(KsefClient, upoDownloadUrl);
        Assert.False(string.IsNullOrWhiteSpace(invoiceUpoXml));
        InvoiceUpo invoiceUpo = UpoUtils.UpoParse<InvoiceUpo>(invoiceUpoXml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);

        // 8) UPO faktury po numerze KSeF
        invoiceUpoXml = await UpoUtils.GetSessionInvoiceUpoAsync(KsefClient, openSessionResponse.ReferenceNumber, ksefNumber, accessToken);
        Assert.False(string.IsNullOrWhiteSpace(invoiceUpoXml));
        invoiceUpo = UpoUtils.UpoParse<InvoiceUpo>(invoiceUpoXml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);

        // 9) UPO sesji po numerze referencyjnym UPO
        invoiceUpoXml = await UpoUtils.GetSessionUpoAsync(KsefClient, openSessionResponse.ReferenceNumber, upoReferenceNumber, accessToken);
        Assert.False(string.IsNullOrWhiteSpace(invoiceUpoXml));
        SessionUpo sessionUpo = UpoUtils.UpoParse<SessionUpo>(invoiceUpoXml);
        Assert.Equal(sessionUpo.ReferenceNumber, openSessionResponse.ReferenceNumber);
    }


    /// <summary>
    /// Buduje i wysyła żądanie otwarcia sesji interaktywnej na podstawie danych szyfrowania.
    /// Zwraca odpowiedź z numerem referencyjnym sesji.
    /// </summary>
    private async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(EncryptionData encryptionData, SystemCode systemCode = DefaultSystemCode)
    {
        OpenOnlineSessionRequest openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: DefaultSchemaVersion, value: DefaultFormCodeValue)
            .WithEncryption(
                encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryptionData.EncryptionInfo.InitializationVector)
            .Build();

        OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken, CancellationToken);
        return openOnlineSessionResponse;
    }

    /// <summary>
    /// Przygotowuje fakturę z szablonu, szyfruje ją i wysyła w ramach sesji interaktywnej.
    /// Zwraca odpowiedź z numerem referencyjnym faktury.
    /// </summary>
    private async Task<SendInvoiceResponse> SendEncryptedInvoiceAsync(
        string sessionReferenceNumber,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService,
        string invoiceTemplatePath,
        string nip)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", invoiceTemplatePath);
        string xml = File.ReadAllText(path, Encoding.UTF8);
        xml = xml.Replace("#nip#", nip);
        xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");

        using MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        byte[] invoice = memoryStream.ToArray();

        byte[] encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
        FileMetadata invoiceMetadata = cryptographyService.GetMetaData(invoice);
        FileMetadata encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        SendInvoiceResponse sendInvoiceResponse = await KsefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken);
        return sendInvoiceResponse;
    }

    /// <summary>
    /// Wykonuje pobieranie statusu sesji do momentu, aż licznik poprawnych faktur nie będzie null.
    /// Zwraca aktualny status sesji.
    /// </summary>
    private async Task<SessionStatusResponse> GetSessionStatusUntilInvoiceCountAvailableAsync(string sessionReferenceNumber)
    {
        SessionStatusResponse? statusResponse;
        do
        {
            statusResponse = await KsefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken);
            await Task.Delay(SleepTime);
        } while (statusResponse.SuccessfulInvoiceCount is null);

        return statusResponse;
    }

    /// <summary>
    /// Zamyka istniejącą sesję interaktywną.
    /// </summary>
    private async Task CloseOnlineSessionAsync(string sessionReferenceNumber)
    {
        await KsefClient.CloseOnlineSessionAsync(sessionReferenceNumber, accessToken);
    }

    /// <summary>
    /// Pobiera bieżący status sesji interaktywnej.
    /// </summary>
    private async Task<SessionStatusResponse> GetSessionStatusAsync(string sessionReferenceNumber)
    {
        return await KsefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken);
    }

    /// <summary>
    /// Pobiera listę metadanych faktur przesłanych w ramach sesji interaktywnej.
    /// </summary>
    private async Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string sessionReferenceNumber)
    {
        return await KsefClient.GetSessionInvoicesAsync(sessionReferenceNumber, accessToken);
    }
}