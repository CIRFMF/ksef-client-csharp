using KSeF.Client.Api.Services;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.QrCode;

public class QrCodeOnlineE2EScenarioFixture
{
    public string? AccessToken { get; set; }
    public string? Nip { get; set; }
    public string? SessionReferenceNumber { get; set; }
}

[CollectionDefinition("QrCodeOnlineE2EScenario")]
public class QrCodeOnlineE2EScenarioCollection
: ICollectionFixture<QrCodeOnlineE2EScenarioFixture>
{ }
[Collection("QrCodeOnlineE2EScenario")]
public class QrCodeOnlineE2ETests : TestBase
{
    private readonly QrCodeOnlineE2EScenarioFixture _fixture;
    private readonly EncryptionData _encryptionData;
    private readonly QrCodeService _qrSvc;
    private readonly VerificationLinkService _linkSvc;

    public QrCodeOnlineE2ETests(QrCodeOnlineE2EScenarioFixture fixture)
    {
        _fixture = fixture;
        _linkSvc = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnvironmentsUris.TEST });
        _encryptionData = CryptographyService.GetEncryptionData();
        _qrSvc = new QrCodeService();
        _fixture.Nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, _fixture.Nip).GetAwaiter().GetResult();
        _fixture.AccessToken = authInfo.AccessToken.Token;
    }

    [Theory]
    [InlineData([SystemCode.FA2, "invoice-template-fa-2.xml"])]
    [InlineData([SystemCode.FA3, "invoice-template-fa-3.xml"])]
    public async Task QrCodeOnlineE2ETest(SystemCode systemCode, string invoiceTemplate)
    {
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            _encryptionData,
            _fixture.AccessToken!,
            systemCode);
        Assert.NotNull(openSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));

        _fixture.SessionReferenceNumber = openSessionResponse.ReferenceNumber;

        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _fixture.AccessToken,
            _fixture.Nip,
            invoiceTemplate,
            _encryptionData,
            CryptographyService);

        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));

        SessionStatusResponse sessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _fixture.AccessToken);

        Assert.NotNull(sessionStatus);
        Assert.True(sessionStatus.Status.Code > 0);
        Assert.NotNull(sessionStatus.InvoiceCount);

        await OnlineSessionUtils.CloseOnlineSessionAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _fixture.AccessToken);

        const int MaxSessionStatusAttempts = 120;
        sessionStatus = await AsyncPollingUtils.PollAsync(
            action: async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _fixture.AccessToken),
            condition: s => s.Status.Code != OnlineSessionCodeResponse.SessionClosed,
            description: "Oczekiwanie na zakończenie przetwarzania sesji (status != 170)",
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxSessionStatusAttempts
        );

        Assert.NotEqual(OnlineSessionCodeResponse.NoValidInvoices, sessionStatus.Status.Code);

        SessionInvoice invoicesStatus = await KsefClient.GetSessionInvoiceAsync(
            _fixture.SessionReferenceNumber,
            sendInvoiceResponse.ReferenceNumber,
            _fixture.AccessToken);

        Assert.NotNull(invoicesStatus);

        const int MaxInvoiceStatusAttempts = 15;
        invoicesStatus = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.GetSessionInvoiceAsync(
                _fixture.SessionReferenceNumber,
                sendInvoiceResponse.ReferenceNumber,
                _fixture.AccessToken),
            condition: inv => inv.Status.Code != 150,
            description: "Oczekiwanie na zakończenie przetwarzania faktury (status != 150)",
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxInvoiceStatusAttempts
        );

        Assert.Equal(OnlineSessionCodeResponse.ProcessedSuccessfully, invoicesStatus.Status.Code);

        SessionInvoicesResponse invoicesMetadata = await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            _fixture.AccessToken);

        Assert.NotNull(invoicesMetadata);
        Assert.NotEmpty(invoicesMetadata.Invoices);

        SessionInvoice invoiceMetadata = invoicesMetadata.Invoices.Single(x => x.ReferenceNumber == sendInvoiceResponse.ReferenceNumber);
        string invoiceKsefNumber = invoiceMetadata.KsefNumber;
        string invoiceHash = invoiceMetadata.InvoiceHash;
        DateTimeOffset invoicingDate = invoiceMetadata.InvoicingDate;

        string invoiceForOnlineUrl = _linkSvc.BuildInvoiceVerificationUrl(_fixture.Nip, invoicingDate.DateTime, invoiceHash);

        Assert.NotNull(invoiceForOnlineUrl);
        Assert.Contains(Convert.FromBase64String(invoiceHash).EncodeBase64UrlToString(), invoiceForOnlineUrl);
        Assert.Contains(_fixture.Nip, invoiceForOnlineUrl);
        Assert.Contains(invoicingDate.ToString("dd-MM-yyyy"), invoiceForOnlineUrl);

        byte[] qrOnline = _qrSvc.GenerateQrCode(invoiceForOnlineUrl);
        Assert.NotNull(qrOnline);

        qrOnline = _qrSvc.AddLabelToQrCode(qrOnline, invoiceKsefNumber);
        Assert.NotEmpty(qrOnline);
    }
}