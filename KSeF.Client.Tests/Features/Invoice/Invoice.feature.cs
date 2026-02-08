#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
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
            Core.Models.Authorization.AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip).GetAwaiter().GetResult();
            authToken = authInfo.AccessToken.Token;
        }

        [Theory]
        [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        [Trait("Scenario", "Posiadając uprawnienie właścicielskie pytamy o fakturę wysłaną")]
        public async Task GivenNewInvoiceSendedToKsefThenReturnsNewKsefNumber(SystemCode systemCode, string invoiceTemplatePath)
        {
            // uwierzytelnienie w konstruktorze
            Assert.NotNull(authToken);

            // rozpoczęcie sesji online dla faktury
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
                CryptographyService,
                true);
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

                    // Polling zamiast stałego opóźnienia: czekamy aż faktura będzie dostępna do pobrania
                    string invoice = await AsyncPollingUtils.PollAsync(
                        action: async () => await KsefClient.GetInvoiceAsync(item.KsefNumber, authToken).ConfigureAwait(false),
                        condition: result => !string.IsNullOrWhiteSpace(result),
                        description: "Oczekiwanie na możliwość pobrania treści faktury",
                        delay: TimeSpan.FromMilliseconds(SleepTime),
                        maxAttempts: 120);

                    Assert.False(string.IsNullOrWhiteSpace(invoice));
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
        public async Task GivenInvalidNewInvoiceSendedToKsefThenReturnsErrorInvalidKsefNumber(SystemCode systemCode, string invoiceTemplatePath)
        {
            string wrongNIP = MiscellaneousUtils.GetRandomNip();

            // uwierzytelnienie w konstruktorze
            Assert.NotNull(authToken);

            // rozpoczęcie sesji online dla faktury
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
            CryptographyService,
            true);
            Assert.NotNull(sendInvoiceResponse);
            Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

            try
            {
                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
                    async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
                        KsefClient,
                        openSessionRequest.ReferenceNumber,
                        sendInvoiceResponse.ReferenceNumber,
                        authToken).ConfigureAwait(false),
                    result => result is not null && result.Status.Code != InvoiceInSessionStatusCodeResponse.Processing,
                    description: "Oczekiwanie na końcowy status dla nieprawidłowego NIP",
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 120);

                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvalidPermissions, sendInvoiceStatus.Status.Code);
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
        [Trait("Scenario", "Odrzucenie faktury gdy DataWytworzeniaFa < 2025-09-01")]
        public async Task GivenInvoiceWithDataWytworzeniaFaBeforeCutoffShouldFail(
            SystemCode systemCode, string templatePath)
        {
            DateTime cutoffUtc = new(2025, 8, 31);

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

                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
                    async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
                        KsefClient,
                        openSessionRequest.ReferenceNumber,
                        sendInvoiceResponse.ReferenceNumber,
                        authToken).ConfigureAwait(false),
                    result => result is not null && result.Status.Code == (int)InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError,
                    description: "Oczekiwanie na błąd walidacji semantycznej (450) dla DataWytworzeniaFa",
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 120);
                
                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError, sendInvoiceStatus.Status.Code); // KOD 450, błąd walidacji semantycznej dokumentu faktury
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
        [Trait("Scenario", "Odrzucenie faktury gdy P_1 jest ustawione na przyszłą datę")]
        public async Task GivenInvoiceWithP1InFutureShouldFail(
            SystemCode systemCode, string templatePath)
        {
            Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(KsefClient, encryptionData, authToken, systemCode);
            Assert.False(string.IsNullOrWhiteSpace(openSessionRequest?.ReferenceNumber));

            string template = InvoiceHelpers.GetTemplateText(templatePath, nip);

            // Jutro (UTC)
            DateTime tomorrowUtc = DateTime.UtcNow.Date.AddDays(1);
            string invalidXml = InvoiceHelpers.SetElementValue(template, "P_1", InvoiceHelpers.SetDateForElement("P_1", tomorrowUtc));

            try
            {
                Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceFromXmlAsync(
                KsefClient, openSessionRequest.ReferenceNumber, authToken, invalidXml, encryptionData, CryptographyService);
                Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse?.ReferenceNumber));

                Core.Models.Sessions.SessionInvoice sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
                    async () => await OnlineSessionUtils.GetSessionInvoiceStatusAsync(
                        KsefClient,
                        openSessionRequest.ReferenceNumber,
                        sendInvoiceResponse.ReferenceNumber,
                        authToken).ConfigureAwait(false),
                    result => result is not null && result.Status.Code != InvoiceInSessionStatusCodeResponse.Processing,
                    description: "Oczekiwanie na końcowy status dla przyszłej daty P_1",
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 120);

                Assert.NotNull(sendInvoiceStatus);
                Assert.Equal(InvoiceInSessionStatusCodeResponse.InvoiceSemanticValidationError, sendInvoiceStatus.Status.Code); // KOD 450, błąd walidacji semantycznej dokumentu faktury
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
        [Trait("Scenario", "QueryInvoiceMetadataAsync zgłasza wyjątek gdy pageSize jest poza dozwolonym zakresem")]
        public async Task GivenInvoiceMetadataQueryWithInvalidPageSizeShouldThrowError(int pageSize)
        {
            // uwierzytelnienie w konstruktorze
            Assert.NotNull(authToken);

            InvoiceQueryFilters invoiceMetadataQueryRequest = new()
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

        [Fact]
        [Trait("Scenario", "Podmiot 3 może wyszukać fakturę, w której został wskazany jako ThirdSubject")]
        public async Task GivenNewInvoiceWithP3_SendedToKsef_ThenP3ShouldFoundInvoice()
        {
            // Przygotowanie
            EncryptionData encryptionData = CryptographyService.GetEncryptionData();
            string invoiceCreatorNip = MiscellaneousUtils.GetRandomNip();
            string thirdSubjectIdentifier = MiscellaneousUtils.GetRandomNip();

            // Wystawienie faktury przez wykonawcę dla jednostki podrzędnej identyfikującej sie numerem NIP  ---
            string invoiceCreatorAuthToken = (await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient, invoiceCreatorNip)).AccessToken.Token;

            OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
                KsefClient,
                encryptionData,
                invoiceCreatorAuthToken,
                SystemCode.FA3);

            Assert.NotNull(openSessionResponse?.ReferenceNumber);

            SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                invoiceCreatorAuthToken,
                invoiceCreatorNip,
                thirdSubjectIdentifier,
                "invoice-template-fa-3-with-custom-Subject3.xml",
                encryptionData,
                CryptographyService,
                true);

            Assert.NotNull(sendInvoiceResponse);

            SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
                async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                    KsefClient,
                    openSessionResponse.ReferenceNumber,
                    invoiceCreatorAuthToken).ConfigureAwait(false),
                result => result is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
                delay: TimeSpan.FromMilliseconds(2 * SleepTime));

            Assert.NotNull(sessionStatus);
            Assert.Equal(sessionStatus.InvoiceCount, sessionStatus.SuccessfulInvoiceCount);

            SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(
                openSessionResponse.ReferenceNumber,
                invoiceCreatorAuthToken,
                pageSize: 10);

            Assert.NotEmpty(invoices.Invoices);

            // wyszukiwanie faktury jako podmiot3---
            AuthenticationOperationStatusResponse autResult = await AuthenticationUtils.AuthenticateAsOrganizationAsync(KsefClient, thirdSubjectIdentifier, AuthenticationTokenContextIdentifierType.Nip, EncryptionMethodEnum.ECDsa);
            string thirdSubjectToken = autResult.AccessToken.Token;

            await Task.Delay(10 * SleepTime);

            InvoiceQueryFilters invoiceQuery = new()
            {
                SubjectType = InvoiceSubjectType.Subject3,
                DateRange = new DateRange
                {
                    From = DateTime.UtcNow.AddDays(-2),
                    To = DateTime.UtcNow.AddDays(1),
                    DateType = DateType.PermanentStorage
                }
            };


            PagedInvoiceResponse invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
                requestPayload: invoiceQuery,
                accessToken: thirdSubjectToken,
                cancellationToken: CancellationToken.None,
                pageOffset: 0,
                pageSize: 30);

            // Asercje
            Assert.Contains(invoiceQueryResponse.Invoices, x => x.ThirdSubjects.Any(y => y.Identifier.Value == thirdSubjectIdentifier));
        }
    }
}