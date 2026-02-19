#nullable enable
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Utils;
using System.Globalization;
using System.Xml.Linq;

namespace KSeF.Client.Tests.Core.E2E.QrCode;

public class QrCodeOnlineE2EScenarioFixture
{
    public string AccessToken { get; set; }
    public string Nip { get; set; }
    public string SessionReferenceNumber { get; set; }
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
    private readonly VerificationLinkService _linkSvc;

    public QrCodeOnlineE2ETests(QrCodeOnlineE2EScenarioFixture fixture)
    {
        _fixture = fixture;
        _linkSvc = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnvironmentsUris.TEST, BaseQRUrl = KsefQREnvironmentsUris.TEST });
        _encryptionData = CryptographyService.GetEncryptionData();
        _fixture.Nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, _fixture.Nip).GetAwaiter().GetResult();
        _fixture.AccessToken = authInfo.AccessToken.Token;
    }

    [Theory]
    [InlineData(new object[] { SystemCode.FA2, "invoice-template-fa-2.xml" })]
    [InlineData(new object[] { SystemCode.FA3, "invoice-template-fa-3.xml" })]
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
            CryptographyService,
            true);

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
                _fixture.AccessToken).ConfigureAwait(false),
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
                _fixture.AccessToken).ConfigureAwait(false),
            condition: inv => inv.Status.Code != InvoiceInSessionStatusCodeResponse.Processing,
            description: "Oczekiwanie na zakończenie przetwarzania faktury",
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

        DateTime issueDateFromTemplate = GetIssueDateFromTemplate(invoiceTemplate);

        string invoiceForOnlineUrl = _linkSvc.BuildInvoiceVerificationUrl(_fixture.Nip, issueDateFromTemplate, invoiceHash);

        Assert.NotNull(invoiceForOnlineUrl);
        Assert.Contains(Convert.FromBase64String(invoiceHash).EncodeBase64UrlToString(), invoiceForOnlineUrl);
        Assert.Contains(_fixture.Nip, invoiceForOnlineUrl);
        Assert.Contains(issueDateFromTemplate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), invoiceForOnlineUrl);

        byte[] qrOnline = QrCodeService.GenerateQrCode(invoiceForOnlineUrl);
        Assert.NotNull(qrOnline);

        qrOnline = QrCodeService.AddLabelToQrCode(qrOnline, invoiceKsefNumber);
        Assert.NotEmpty(qrOnline);
    }

    private static DateTime GetIssueDateFromTemplate(string templateName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Nie znaleziono szablonu: {path}");
        }

        string xml = File.ReadAllText(path);
        XDocument doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

        string? p1 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "P_1")?.Value;
        if (!string.IsNullOrWhiteSpace(p1) && DateTime.TryParseExact(p1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime issueDate))
        {
            return issueDate;
        }

        string? created = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DataWytworzeniaFa")?.Value;
        if (!string.IsNullOrWhiteSpace(created) && DateTime.TryParse(created, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime createdDt))
        {
            return createdDt.Date;
        }

        throw new InvalidOperationException($"Nie można odczytać daty wystawienia z szablonu {templateName}.");
    }
}