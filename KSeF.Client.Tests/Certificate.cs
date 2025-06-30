using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Certificates;
using KSeFClient.Api.Builders.Certificates;

namespace KSeF.Client.Tests
{
    public class CertificateScenarioFixture
    {
        public string AccessToken { get; set; }
        public CertificateLimitResponse Limits { get; set; }
        public CertificateEnrollmentsInfoResponse EnrollmentInfo { get; set; }
        public string EnrollmentReference { get; set; }
        public CertificateEnrollmentStatusResponse EnrollmentStatus { get; set; }
        public List<string> SerialNumbers { get; set; }
        public CertificateListResponse RetrievedCertificates { get; set; }
        public CertificateMetadataListResponse MetadataList { get; set; }
    }

    [CollectionDefinition("CertificateScenario")]
    public class CertificateScenarioCollection : ICollectionFixture<CertificateScenarioFixture> { }

    [Collection("CertificateScenario")]
    public class CertificateE2ETests : TestBase
    {
        private readonly CertificateScenarioFixture _testFixture;

        public CertificateE2ETests(CertificateScenarioFixture f)
        {
            _testFixture = f;
            _testFixture.AccessToken = AccessToken;
        }

        [Fact]
        public async Task CertificateController_E2E_WorksCorrectly()
        {
            // 1. Pobierz limity certyfikatów
            await Step1_GetLimitsAsync();

            // 2. Pobierz info o rejestracji
            await Step2_GetEnrollmentInfoAsync();

            // 3. Wyślij zgłoszenie certyfikatu
            await Step3_SendEnrollmentAsync();

            // 4. Sprawdź status rejestracji
            await Step4_GetEnrollmentStatusAsync();

            // 5. Pobierz wygenerowany certyfikat
            await Step5_GetCertificateAsync();

            // 6. Cofnij certyfikat
            await Step6_RevokeCertificateAsync();

            // 7. Pobierz listę metadanych
            await Step7_GetMetadataListAsync();
        }

        public async Task Step1_GetLimitsAsync()
        {
            var resp = await kSeFClient
                .GetCertificateLimitsAsync(_testFixture.AccessToken, CancellationToken.None);
            Assert.NotNull(resp);
            Assert.True(resp.CanRequest);
            _testFixture.Limits = resp;
        }

        public async Task Step2_GetEnrollmentInfoAsync()
        {
            var resp = await kSeFClient
                .GetCertificateEnrollmentDataAsync(_testFixture.AccessToken, CancellationToken.None);
            Assert.NotNull(resp);
            Assert.NotEmpty(resp.SerialNumber);
            _testFixture.EnrollmentInfo = resp;
        }

        public async Task Step3_SendEnrollmentAsync()
        {

            var cryptographyService = new CryptographyService(kSeFClient, restClient) as ICryptographyService;
            var (csr, key) = cryptographyService.GenerateCsr(_testFixture.EnrollmentInfo);

            var req = SendCertificateEnrollmentRequestBuilder
                .Create()
                .WithCertificateName("E2E Test Cert")
                .WithCsr(csr)
                .WithValidFrom(DateTimeOffset.UtcNow.AddDays(1))
                .Build();

            var resp = await kSeFClient
                .SendCertificateEnrollmentAsync(req, _testFixture.AccessToken, CancellationToken.None);
            Assert.NotNull(resp);
            Assert.False(string.IsNullOrWhiteSpace(resp.ReferenceNumber));
            _testFixture.EnrollmentReference = resp.ReferenceNumber;
        }

        public async Task Step4_GetEnrollmentStatusAsync()
        {
            var resp = await kSeFClient
                .GetCertificateEnrollmentStatusAsync(_testFixture.EnrollmentReference, _testFixture.AccessToken, CancellationToken.None);
            Assert.NotNull(resp);

            while(resp.Status.Code == 100)
            {
                await Task.Delay(1000);
                resp = await kSeFClient
                                .GetCertificateEnrollmentStatusAsync(_testFixture.EnrollmentReference, _testFixture.AccessToken, CancellationToken.None);
                
            }
            Assert.True(resp.Status.Code == 200 );
            _testFixture.EnrollmentStatus = resp;
        }

        public async Task Step5_GetCertificateAsync()
        {
            _testFixture.SerialNumbers = new List<string> { _testFixture.EnrollmentStatus.CertificateSerialNumber };
            var resp = await kSeFClient
                .GetCertificateListAsync(new CertificateListRequest { CertificateSerialNumbers = _testFixture.SerialNumbers }, _testFixture.AccessToken, CancellationToken.None);
            Assert.NotNull(resp);
            //TODO
            Assert.Single(resp.Certificates);
            _testFixture.RetrievedCertificates = resp;
        }

        public async Task Step6_RevokeCertificateAsync()
        {
            var serial = _testFixture.RetrievedCertificates.Certificates.ToList().First().CertificateSerialNumber;

            var req = RevokeCertificateRequestBuilder
                .Create()
                .WithRevocationReason(CertificateRevocationReason.KeyCompromise)
                .Build();

            await kSeFClient
                .RevokeCertificateAsync(req, serial, _testFixture.AccessToken, CancellationToken.None);
        }

        public async Task Step7_GetMetadataListAsync()
        {
            var resp = await kSeFClient
                .GetCertificateMetadataListAsync(_testFixture.AccessToken, null, null, null, CancellationToken.None);
            Assert.NotNull(resp);
            Assert.Contains(resp.Certificates.ToList(), m => _testFixture.SerialNumbers.Contains(m.CertificateSerialNumber));
            _testFixture.MetadataList = resp;
        }
    }
}
