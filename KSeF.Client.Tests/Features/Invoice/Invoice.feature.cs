using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [CollectionDefinition("Invoice.feature")]
    [Trait("Category", "Features")]
    [Trait("Features", "Invoice.feature")]
    public partial class InvoiceTests : KsefIntegrationTestBase
    {
        private string authToken { get; set; }
        private string nip { get; set; }

        public InvoiceTests()
        {
            nip = MiscellaneousUtils.GetRandomNip();
            Core.Models.Authorization.AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, nip).GetAwaiter().GetResult();
            authToken = authInfo.AccessToken.Token;
        }

        [Theory]
        [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        [Trait("Scenario", "Posiadając uprawnienie właścicielskie pytamy o fakturę wysłaną")]
        public async Task GivenNewInvoice_SendedToKsef_ThenReturnsNewKsefNumber(SystemCode systemCode, string invoiceTemplatePath)
        {
            // authenticated in constructor
            Assert.NotNull(authToken);

            // proceed with invoice online session
            Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient,
                encryptionData,
                authToken,
                systemCode);
            Assert.NotNull(openSessionRequest);
            Assert.NotNull(openSessionRequest.ReferenceNumber);

            Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(KsefClient,
                openSessionRequest.ReferenceNumber,
                authToken,
                nip,
                invoiceTemplatePath,
                encryptionData,
                CryptographyService);
            Assert.NotNull(sendInvoiceResponse);
            Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

            try
            {
                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await OnlineSessionUtils.GetSessionInvoiceStatusAsync(KsefClient,
                    openSessionRequest.ReferenceNumber,
                    sendInvoiceResponse.ReferenceNumber,
                    authToken);
                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(200, sendInvoiceStatus.Status.Code);

                Core.Models.Sessions.SessionInvoicesResponse sessionInvoices = await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(KsefClient,
                openSessionRequest.ReferenceNumber,
                authToken);
                Assert.NotNull(sessionInvoices);
                Assert.NotEmpty(sessionInvoices.Invoices);

                foreach (Core.Models.Sessions.SessionInvoice? item in sessionInvoices.Invoices)
                {
                    string invoiceMetadata = await OnlineSessionUtils.GetSessionInvoiceUpoAsync(KsefClient,
                    openSessionRequest.ReferenceNumber,
                    item.KsefNumber,
                    authToken);
                    Assert.NotNull(invoiceMetadata);

                    await Task.Delay(60000);

                    string invoice = await KsefClient.GetInvoiceAsync(item.KsefNumber, authToken);
                    Assert.NotNull(invoice);
                }
            }
            finally
            {
                await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, openSessionRequest.ReferenceNumber, authToken);
            }
        }

        [Theory]
        [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        [Trait("Scenario", "Posiadając uprawnienie właścicielskie wysyłamy szyfrowaną fakturę z nieprawidłowym numerem NIP sprzedawcy")]
        public async Task GivenInvalidNewInvoice_SendedToKsef_ThenReturnsErrorInvalidKsefNumber(SystemCode systemCode, string invoiceTemplatePath)
        {
            string wrongNIP = MiscellaneousUtils.GetRandomNip();

            // authenticated in constructor
            Assert.NotNull(authToken);

            // proceed with invoice online session
            Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient,
                encryptionData,
                authToken,
                systemCode);

            Assert.NotNull(openSessionRequest);
            Assert.NotNull(openSessionRequest.ReferenceNumber);

            Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(KsefClient,
            openSessionRequest.ReferenceNumber,
            authToken,
            wrongNIP,
            invoiceTemplatePath,
            encryptionData,
            CryptographyService);
        Assert.NotNull(sendInvoiceResponse);
        Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

            try
            {
                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await OnlineSessionUtils.GetSessionInvoiceStatusAsync(KsefClient,
                    openSessionRequest.ReferenceNumber,
                    sendInvoiceResponse.ReferenceNumber,
                    authToken);
                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvalidPermissions, sendInvoiceStatus.Status.Code); // CODE 410, Insufficient permissions
            }
            finally
            {
                await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, openSessionRequest.ReferenceNumber, authToken);
            }
        }

        [Theory]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        [Trait("Category", "SchemaValidationError")]
        [Trait("Field", "DataWytworzeniaFa")]
        [Trait("Condition", "< 2025-09-01")]
        [Trait("Scenario", "Invoice rejected when DataWytworzeniaFa < 2025-09-01")]
        public async Task GivenInvoice_WithDataWytworzeniaFa_BeforeCutoff_ShouldFail(
            SystemCode systemCode, string templatePath)
        {
            DateTime cutoffUtc = new DateTime(2025, 8, 31);

            Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, authToken, systemCode);
            Assert.False(string.IsNullOrWhiteSpace(openSessionRequest?.ReferenceNumber));

            string template = InvoiceHelpers.GetTemplateText(templatePath, nip);

            string invalidXml = InvoiceHelpers.SetElementValue(
                template, "DataWytworzeniaFa", InvoiceHelpers.SetDateForElement("DataWytworzeniaFa", cutoffUtc));

        try
        {
                Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceFromXmlAsync(
                KsefClient, openSessionRequest.ReferenceNumber, authToken, invalidXml, encryptionData, CryptographyService);
            Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse?.ReferenceNumber));

                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await OnlineSessionUtils.GetSessionInvoiceStatusAsync(KsefClient,
                    openSessionRequest.ReferenceNumber,
                    sendInvoiceResponse.ReferenceNumber,
                    authToken);
                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError, sendInvoiceStatus.Status.Code); // CODE 450, Semantic validation error of the invoice document
            }
            finally
            {
                await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, openSessionRequest.ReferenceNumber, authToken);
            }
        }

        [Theory]
        [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        [Trait("ErrorType", "SchemaValidationError")]
        [Trait("Field", "P_1")]
        [Trait("Condition", "> today")]
        [Trait("Scenario", "Invoice rejected when P_1 is set to a future date")]
        public async Task GivenInvoice_WithP1_InFuture_ShouldFail(
            SystemCode systemCode, string templatePath)
        {
            Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, authToken, systemCode);
            Assert.False(string.IsNullOrWhiteSpace(openSessionRequest?.ReferenceNumber));

            string template = InvoiceHelpers.GetTemplateText(templatePath, nip);

            // Tomorrow UTC
            DateTime tomorrowUtc = DateTime.UtcNow.Date.AddDays(1);
            string invalidXml = InvoiceHelpers.SetElementValue(template, "P_1", InvoiceHelpers.SetDateForElement("P_1", tomorrowUtc));

        try
        {
                Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceFromXmlAsync(
                KsefClient, openSessionRequest.ReferenceNumber, authToken, invalidXml, encryptionData, CryptographyService);
            Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse?.ReferenceNumber));

                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await OnlineSessionUtils.GetSessionInvoiceStatusAsync(KsefClient,
                    openSessionRequest.ReferenceNumber,
                    sendInvoiceResponse.ReferenceNumber,
                    authToken);
                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError, sendInvoiceStatus.Status.Code); // CODE 450, Semantic validation error of the invoice document
            }
            finally
            {
                await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, openSessionRequest.ReferenceNumber, authToken);
            }
        }

        [Theory]
        [InlineData(9)]
        [InlineData(251)]
        [InlineData(999)]
        [Trait("Category", "InvoiceMetadata")]
        [Trait("ErrorType", "ValidationError")]
        [Trait("Field", "pageSize")]
        [Trait("Condition", "outside 10-250")]
        [Trait("Scenario", "QueryInvoiceMetadataAsync throws when pageSize is out of allowed range")]
        public async Task GivenInvoiceMetadataQuery_WithInvalidPageSize_ShouldThrowError(int pageSize)
        {
            // authenticated in constructor
            Assert.NotNull(authToken);

            InvoiceQueryFilters invoiceMetadataQueryRequest = new InvoiceQueryFilters
            {
                SubjectType = InvoiceSubjectType.Subject1,
                DateRange = new DateRange
                {
                    From = DateTime.UtcNow.AddDays(-30),
                    To = DateTime.UtcNow,
                    DateType = DateType.Issue
                }
            };

            await Assert.ThrowsAnyAsync<KsefApiException>(() =>
                KsefClient.QueryInvoiceMetadataAsync(
                    requestPayload: invoiceMetadataQueryRequest,
                    accessToken: authToken,
                    cancellationToken: CancellationToken.None,
                    pageOffset: 0,
                    pageSize: pageSize));
        }
    }
}