using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;

namespace KSeF.Client.Tests
{
    public class EuEntityRepresentativeScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier EuEntity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Fingerprint,
            Value = "00987654321"
        };
        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        public PagedPermissionsResponse<EuEntityPermission> SearchResponse { get; set; }
    }

    [CollectionDefinition("EuEntityRepresentativeScenario")]
    public class EuEntityRepresentativeScenarioCollection
        : ICollectionFixture<EuEntityRepresentativeScenarioFixture>
    { }

    [Collection("EuEntityRepresentativeScenario")]
    public class EuEntityRepresentativePermissionE2ETests : TestBase
    {
        private readonly EuEntityRepresentativeScenarioFixture _f;

        public EuEntityRepresentativePermissionE2ETests(EuEntityRepresentativeScenarioFixture f):base(Core.Models.Authorization.ContextIdentifierType.NipVatUe)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.EuEntity.Value = "73" + randomGenerator.Next(100000000, 999999999);
        }

        [Fact]
        public async Task EuEntityRepresentative_E2E_GrantSearchRevokeSearch()
        {
            await Step1_GrantEuRepAsync();
            Thread.Sleep(sleepTime);

            await Step2_SearchEuRepAsync(expectAny: true);
            Thread.Sleep(sleepTime);

            await Step3_RevokeEuRepAsync();
            Thread.Sleep(sleepTime);

            await Step4_SearchEuRepAsync(expectAny: false);
        }

        public async Task Step1_GrantEuRepAsync()
        {
            var req = GrantEUEntityRepresentativePermissionsRequestBuilder
                .Create()
                .WithSubject(_f.EuEntity)
                .WithPermissions(StandardPermissionType.InvoiceWrite, StandardPermissionType.InvoiceRead)
                .WithDescription("E2E EU representative")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionEUEntityRepresentativeAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchEuRepAsync(bool expectAny)
        {
            var query = new Core.Models.Permissions.EUEntity.EuEntityPermissionsQueryRequest { /* filtrowanie */ };
            var resp = await kSeFClient
                .SearchGrantedEuEntityPermissionsAsync(
                    query, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.NotEmpty(resp.Permissions);
                _f.SearchResponse = resp;
            }
            else
            {
                Assert.Empty(resp.Permissions);
            }
        }

        public async Task Step3_RevokeEuRepAsync()
        {
            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await kSeFClient
                .RevokeAuthorizationsPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
                _f.RevokeResponse.Add(resp);
            }            
        }

        public async Task Step4_SearchEuRepAsync(bool expectAny)
            => await Step2_SearchEuRepAsync(expectAny);
    }
}
