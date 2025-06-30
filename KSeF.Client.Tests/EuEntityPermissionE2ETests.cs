
using KSeF.Client.Api.Builders.EUEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;

namespace KSeF.Client.Tests
{
    public class EuEntityScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier EuEntity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Fingerprint,
            Value = "EU123456789"
        };
        public OperationResponse GrantResponse { get; set; }
        public IList<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        public PagedPermissionsResponse<EuEntityPermission> SearchResponse { get; set; }
    }

    [CollectionDefinition("EuEntityScenario")]
    public class EuEntityScenarioCollection
        : ICollectionFixture<EuEntityScenarioFixture>
    { }

    [Collection("EuEntityScenario")]
    public class EuEntityPermissionE2ETests : TestBase
    {
        private readonly EuEntityScenarioFixture _f;

        public EuEntityPermissionE2ETests(EuEntityScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.EuEntity.Value = "EU" + randomGenerator.Next(100000000, 999999999);
        }

        [Fact]
        public async Task EuEntity_E2E_GrantSearchRevokeSearch()
        {
            //Dodaje 4, a usuwa tylko dwa mimo, ze mam idki?
            await Step1_GrantEuAsync();
            Thread.Sleep(sleepTime);

            await Step2_SearchEuAsync(expectAny: true);
            Thread.Sleep(sleepTime);

            await Step3_RevokeEuAsync();
            Thread.Sleep(sleepTime);

            await Step4_SearchEuAsync(expectAny: false);
        }

        public async Task Step1_GrantEuAsync()
        {
            var req = GrantEUEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.EuEntity)
                .WithContext(new ContextIdentifier() { Type = ContextIdentifierType.NipVatUe, Value = NIP})
                .WithDescription("E2E EU ")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionEUEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchEuAsync(bool expectAny)
        {
            var query = new EuEntityPermissionsQueryRequest { /* e.g. filtrowanie */ };
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

        public async Task Step3_RevokeEuAsync()
        {
            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await kSeFClient
                .RevokeCommonPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
                _f.RevokeResponse.Add(resp);
            }
            
        }

        public async Task Step4_SearchEuAsync(bool expectAny)
            => await Step2_SearchEuAsync(expectAny);
    }
}
