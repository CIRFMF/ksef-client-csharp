using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.ApiResponses;
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
    private const SystemCode DefaultSystemCode = SystemCode.FA3;
    private const string DefaultSchemaVersion = "1-0E";
    private const string DefaultFormCodeValue = "FA";
    private const int SuccessfulInvoiceCountExpected = 1;

    private readonly string _accessToken = string.Empty;
    private string _nip { get; }

    public UpoE2ETests()
    {
        _nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _nip)
                                          .GetAwaiter()
                                          .GetResult();
        _accessToken = authInfo.AccessToken.Token;
    }

    [Theory]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task UpoRetreivalAsyncFullIntegrationFlowAllStepsSucceed(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // 1) Otwarcie sesji
        OpenOnlineSessionResponse openSessionResponse = await OpenOnlineSessionAsync(encryptionData, systemCode);
        Assert.NotNull(openSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));
        await Task.Delay(SleepTime);

        // 2) Wysłanie faktury
        SendInvoiceResponse sendInvoiceResponse = await SendEncryptedInvoiceAsync(openSessionResponse.ReferenceNumber, encryptionData, CryptographyService, invoiceTemplatePath, _nip);
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
        await Task.Delay(SleepTime);

        // 3) Status po wysłaniu faktury (oczekujemy 1 sukcesu, braku błędów, braku UPO, kodu 100)
        SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.GetSessionStatusAsync(
                openSessionResponse.ReferenceNumber,
                _accessToken
            ).ConfigureAwait(false),
            condition: result => result?.SuccessfulInvoiceCount is not null,
            cancellationToken: CancellationToken
        );

        Assert.NotNull(statusAfterSend);
        Assert.NotNull(statusAfterSend.SuccessfulInvoiceCount);
        Assert.Equal(SuccessfulInvoiceCountExpected, statusAfterSend.SuccessfulInvoiceCount);
        Assert.Null(statusAfterSend.FailedInvoiceCount);
        Assert.Null(statusAfterSend.Upo);
        Assert.Equal(OnlineSessionCodeResponse.SessionOpened, statusAfterSend.Status.Code);
        await Task.Delay(SleepTime);

        // 4) Zamknięcie sesji
        await KsefClient.CloseOnlineSessionAsync(
            openSessionResponse.ReferenceNumber,
            _accessToken
        );

        await Task.Delay(SleepTime);

        // 5) Pobranie faktur sesji (powinna być jedna)
        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(
            openSessionResponse.ReferenceNumber,
            _accessToken
        );

        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices.Invoices);
        Assert.Single(invoices.Invoices);
        string ksefNumber = invoices.Invoices.First().KsefNumber;

        // 6) Status po zamknięciu (kod 200) i numer referencyjny UPO
        SessionStatusResponse statusAfterClose = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.GetSessionStatusAsync(
                openSessionResponse.ReferenceNumber,
                _accessToken
            ).ConfigureAwait(false),
            condition: result => result is not null
                         && result.Status is not null
                         && result.Status.Code == OnlineSessionCodeResponse.ProcessedSuccessfully,
            description: "oczekiwanie na status sesji = 200 (ProcessedSuccessfully)",
            cancellationToken: CancellationToken
        );

        Assert.NotNull(statusAfterClose);
        Assert.Equal(OnlineSessionCodeResponse.ProcessedSuccessfully, statusAfterClose.Status.Code);
        string upoReferenceNumber = statusAfterClose.Upo.Pages.First().ReferenceNumber;

        // 7) pobranie UPO faktury z URL zawartego w metadanych faktury razem z hash z nagłówka
        Uri upoDownloadUrl = invoices.Invoices.First().UpoDownloadUrl;
        UpoWithHash upoWithHash = await UpoUtils.GetUpoWithHashAsync(this.RestClient, upoDownloadUrl, CancellationToken).ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(upoWithHash.Xml));
        Assert.False(string.IsNullOrWhiteSpace(upoWithHash.HashHeaderBase64));

        FileMetadata invoiceMetadataFromUpo = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoWithHash.Xml));
        Assert.Equal(invoiceMetadataFromUpo.HashSHA, upoWithHash.HashHeaderBase64);

        InvoiceUpoV4_3 invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoWithHash.Xml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);

        // 8) UPO faktury po numerze KSeF (z nagłówkiem hash)
        UpoWithHash upoByKsef = await AsyncPollingUtils.PollAsync(
            action: async () => await UpoUtils.GetSessionInvoiceUpoByKsefNumberWithHashAsync(
                RestClient,
                openSessionResponse.ReferenceNumber,
                ksefNumber,
                _accessToken,
                CancellationToken,
                ksefClientFallback: KsefClient).ConfigureAwait(false),
            condition: result => result is not null && !string.IsNullOrWhiteSpace(result.Xml),
            description: "oczekiwanie na dostępność UPO po numerze KSeF",
            cancellationToken: CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(upoByKsef.Xml));
        Assert.False(string.IsNullOrWhiteSpace(upoByKsef.HashHeaderBase64));

        FileMetadata upoByKsefMetadata = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoByKsef.Xml));
        Assert.Equal(upoByKsefMetadata.HashSHA, upoByKsef.HashHeaderBase64);

        invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoByKsef.Xml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);

        // 9) UPO faktury po numerze referencyjnym faktury (z nagłówkiem hash)
        UpoWithHash upoByReference = await AsyncPollingUtils.PollAsync(
            action: async () => await UpoUtils.GetSessionInvoiceUpoByReferenceNumberWithHashAsync(
                RestClient,
                openSessionResponse.ReferenceNumber,
                sendInvoiceResponse.ReferenceNumber,
                _accessToken,
                CancellationToken,
                ksefClientFallback: KsefClient).ConfigureAwait(false),
            condition: result => result is not null && !string.IsNullOrWhiteSpace(result.Xml),
            description: "oczekiwanie na dostępność UPO po numerze referencyjnym faktury",
            cancellationToken: CancellationToken
        );

        Assert.False(string.IsNullOrWhiteSpace(upoByReference.Xml));
        Assert.False(string.IsNullOrWhiteSpace(upoByReference.HashHeaderBase64));

        FileMetadata upoByReferenceMetadata = CryptographyService.GetMetaData(Encoding.UTF8.GetBytes(upoByReference.Xml));
        Assert.Equal(upoByReferenceMetadata.HashSHA, upoByReference.HashHeaderBase64);

        invoiceUpo = UpoUtils.UpoParse<InvoiceUpoV4_3>(upoByReference.Xml);
        Assert.Equal(invoiceUpo.Document.KSeFDocumentNumber, ksefNumber);

        // 10) Zbiorcze UPO sesji po numerze referencyjnym UPO (bez nagłówka hash)
        string sessionUpoXml = await UpoUtils.GetSessionUpoAsync(KsefClient, openSessionResponse.ReferenceNumber, upoReferenceNumber, _accessToken);
        Assert.False(string.IsNullOrWhiteSpace(sessionUpoXml));
        SessionUpoV4_3 sessionUpo = UpoUtils.UpoParse<SessionUpoV4_3>(sessionUpoXml);
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
                initializationVector: encryptionData.EncryptionInfo.InitializationVector,
                publicKeyId: encryptionData.EncryptionInfo.PublicKeyId)
            .Build();

        OpenOnlineSessionResponse openOnlineSessionResponse = await KsefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, _accessToken, cancellationToken: CancellationToken).ConfigureAwait(false);
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

        using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));
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

        SendInvoiceResponse sendInvoiceResponse = await KsefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, _accessToken).ConfigureAwait(false);
        return sendInvoiceResponse;
    }
}