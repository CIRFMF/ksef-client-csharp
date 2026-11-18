using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Invoice
{
    [Collection("InvoicesScenario")]
    public class InvoiceDataVerificationTests : TestBase
    {
        private string _accessToken;
        private string _sellerNip;
        private static readonly string expectedBuyerName = "F.H.U. Jan Kowalski";
        private static readonly string expectedBuyerIdentifierValue = "1111111111";
        private static readonly KSeF.Client.Core.Models.Invoices.BuyerIdentifierType expectedBuyerIdentifierType = KSeF.Client.Core.Models.Invoices.BuyerIdentifierType.Nip;

        private static readonly string expectedSellerName = "Elektrownia S.A.";
        private static readonly string expectedCurrency = "PLN";
        private static readonly decimal expectedVatAmount = (decimal)10.03;
        private static readonly decimal expectedGrossAmount = (decimal)53.63;
        private static readonly decimal expectedNetAmount = (decimal)43.6;
        private static readonly KSeF.Client.Core.Models.Invoices.InvoicingMode expectedInvoicingMode = KSeF.Client.Core.Models.Invoices.InvoicingMode.Offline;

        /// <summary>
        /// Dedykowany test E2E metody QueryInvoiceMetadataAsync.
        /// Kroki:
        /// 1) Uwierzytelnienie sprzedawcy
        /// 2) Otwarcie sesji online
        /// 3) Wysłanie faktury do systemu
        /// 4) Oczekiwanie na przetworzenie faktury i zamknięcie sesji
        /// 5) Pobranie metadanych faktury i weryfikacja pól (nabywca, sprzedawca, kwoty, daty, tryb fakturowania)
        /// </summary>
        [Theory]
        [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
        public async Task Invoice_GetInvoiceMetadataAsync_ReturnsValidatedMetadata(SystemCode systemCode, string invoiceTemplatePath)
        {
            _sellerNip = MiscellaneousUtils.GetRandomNip();
            _accessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, _sellerNip).ConfigureAwait(false)).AccessToken.Token;

            EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            // Krok 1: otwarcie sesji online
            OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
                KsefClient,
                encryptionData,
                _accessToken,
                systemCode);
            Assert.NotNull(openSessionResponse?.ReferenceNumber);
            Assert.True(openSessionResponse?.ValidUntil <= DateTime.UtcNow.AddDays(1));

            // Krok 2: wysłanie faktury
            SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken,
                _sellerNip,
                invoiceTemplatePath,
                encryptionData,
                CryptographyService,
                true);
            Assert.NotNull(sendInvoiceResponse);

            // Krok 3: oczekiwanie na przetworzenie faktur w sesji
            SessionStatusResponse sendInvoiceStatus = await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.GetSessionStatusAsync(
                    openSessionResponse.ReferenceNumber,
                    _accessToken).ConfigureAwait(false),
                result => result?.SuccessfulInvoiceCount is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
                cancellationToken: CancellationToken);
            Assert.NotNull(sendInvoiceStatus);
            Assert.Equal(sendInvoiceStatus.InvoiceCount, sendInvoiceStatus.SuccessfulInvoiceCount);

            // Krok 4: zamknięcie sesji
            await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient,
                 openSessionResponse.ReferenceNumber,
                 _accessToken);

            await Task.Delay(2 * SleepTime);

            // Krok 5: pobranie metadanych sesji
            DateTime permanentlyStoredAtLeast = DateTime.UtcNow.AddMinutes(-2);
            DateTime permanentlyStoredMaxAt = DateTime.UtcNow.AddMinutes(1);

            InvoiceQueryFilters invoiceQuery = new InvoiceQueryFilters
            {
                SubjectType = InvoiceSubjectType.Subject1,
                DateRange = new DateRange
                {
                    From = permanentlyStoredAtLeast,
                    To = permanentlyStoredMaxAt,
                    DateType = DateType.PermanentStorage,
                }
            };

            PagedInvoiceResponse invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
                requestPayload: invoiceQuery,
                accessToken: _accessToken,
                cancellationToken: CancellationToken.None,
                pageOffset: 0,
                pageSize: 30);

            foreach (InvoiceSummary invoice  in invoiceQueryResponse.Invoices)
            {
                Assert.Equal(expectedBuyerName, invoice.Buyer.Name);
                Assert.Equal(expectedBuyerIdentifierValue, invoice.Buyer.Identifier.Value);
                Assert.Equal(expectedBuyerIdentifierType, invoice.Buyer.Identifier.Type);

                Assert.Equal(expectedSellerName, invoice.Seller.Name);
                Assert.Equal(_sellerNip, invoice.Seller.Nip);

                Assert.Equal(expectedCurrency, invoice.Currency);
                Assert.Equal(expectedVatAmount, invoice.VatAmount);
                Assert.Equal(expectedGrossAmount, invoice.GrossAmount);
                Assert.Equal(expectedNetAmount, invoice.NetAmount);

                Assert.True(invoice.PermanentStorageDate >= permanentlyStoredAtLeast && invoice.PermanentStorageDate <= permanentlyStoredMaxAt);
                Assert.True(invoice.AcquisitionDate >= permanentlyStoredAtLeast && invoice.AcquisitionDate <= permanentlyStoredMaxAt);
                Assert.Equal(expectedInvoicingMode, invoice.InvoicingMode);
            }
        }
    }
} 