using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Limits
{

    [Collection("ReteLimits")]
    public class RateLimitsPerIpE2eTests : TestBase
    {
        private const string FirstContextName = "pierwszego kontekstu";
        private const string SecondContextName = "drugiego kontekstu";
        private static readonly RateMax OnlineSessionMax = new(10, 30, 120);

        [Fact]
        public async Task ExceedingPerIpRateLimit_ThrowsKsefRateLimitExceptionAsync()
        {
            string sellerNip = MiscellaneousUtils.GetRandomNip();
            string secondSellerNip = MiscellaneousUtils.GetRandomNip();
            string firstAccessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, sellerNip).ConfigureAwait(false)).AccessToken.Token;
            string secondAccessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, secondSellerNip).ConfigureAwait(false)).AccessToken.Token;
            // Przygotowanie danych szyfrujących i ścieżki do szablonu
            EncryptionData encryptionData = CryptographyService.GetEncryptionData();
            string invoiceTemplatePath = "invoice-template-fa-3.xml";
            string systemCode = SystemCode.FA3.ToString();

            // Uruchom oba zadania kontekstowe równolegle, aby wysłać maksymalną dozwoloną liczbę żądań z tego samego adresu IP
            Task firstBatch = SendBatchForContextAsync(
                firstAccessToken,
                sellerNip,
                invoiceTemplatePath,
                encryptionData,
                CryptographyService,
                systemCode,
                FirstContextName);

            Task secondBatch = SendBatchForContextAsync(
                secondAccessToken,
                secondSellerNip,
                invoiceTemplatePath,
                encryptionData,
                CryptographyService,
                systemCode,
                SecondContextName);

            await Task.WhenAll(firstBatch, secondBatch).ConfigureAwait(false);

            // 3) Po wysłaniu maksymalnej liczby żądań dla obu kontekstów, kolejne żądanie w ramach pierwszego kontekstu powinno zostać odrzucone (limit na kontekst)
            await Assert.ThrowsAsync<KSeF.Client.Core.Exceptions.KsefRateLimitException>(async () =>
            {
                await SendInvoice(firstAccessToken, sellerNip, invoiceTemplatePath, encryptionData, CryptographyService, systemCode).ConfigureAwait(false);
            }).ConfigureAwait(false);

        }

        private async Task SendBatchForContextAsync(
            string accessToken,
            string sellerNip,
            string invoiceTemplatePath,
            EncryptionData encryptionData,
            ICryptographyService CryptographyService,
            string systemCode,
            string contextName)
        {
            try
            {
                for (int i = 0; i < OnlineSessionMax.PerMinute; i++)
                {
                    await SendInvoice(accessToken, sellerNip, invoiceTemplatePath, encryptionData, CryptographyService, systemCode).ConfigureAwait(false);
                    // W celu ominięcia limitu wywołań na sekundę (na sekundę = 10)
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException($"Limit osiągnięto przy pracy na kontekście: {contextName}. Inner: {ex.Message}");
            }
        }


        private async Task SendInvoice(string _accessToken, string _sellerNip, string invoiceTemplatePath, EncryptionData encryptionData, ICryptographyService CryptographyService, string systemCode)
        {
            OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
               KsefClient,
               encryptionData,
               _accessToken).ConfigureAwait(false);
            Assert.NotNull(openSessionResponse?.ReferenceNumber);
            Assert.True(openSessionResponse?.ValidUntil <= DateTime.UtcNow.AddDays(1));

            // wysłanie faktury
            SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                _accessToken,
                _sellerNip,
                invoiceTemplatePath,
                encryptionData,
                CryptographyService,
                true).ConfigureAwait(false);
            Assert.NotNull(sendInvoiceResponse);
        }

    }
}
