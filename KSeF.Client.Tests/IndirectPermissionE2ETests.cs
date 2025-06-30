using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;

namespace KSeF.Client.Tests
{
    public class IndirectPermissionScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier Subject { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };
        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        
        public string Target { get; internal set; }
        public PagedPermissionsResponse<SubunitPermission> SearchResponse { get; internal set; }
    }

    [CollectionDefinition("IndirectPermissionScenario")]
    public class IndirectPermissionScenarioCollection
        : ICollectionFixture<IndirectPermissionScenarioFixture>
    { }

    [Collection("IndirectPermissionScenario")]
    public class IndirectPermissionE2ETests : TestBase
    {
        private readonly IndirectPermissionScenarioFixture _f;

        public IndirectPermissionE2ETests(IndirectPermissionScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Subject.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString();
            _f.Target = randomGenerator
                .Next(900000000, 999999999)
                .ToString();
        }

        [Fact]
        public async Task IndirectPermission_E2E_GrantSearchRevokeSearch()
        {
            await Step1_GrantIndirectAsync();
            Thread.Sleep(sleepTime);

            await Step2_SearchIndirectAsync(expectAny: true);
            Thread.Sleep(sleepTime);

            await Step3_RevokeIndirectAsync();
            Thread.Sleep(sleepTime);

            await Step4_SearchIndirectAsync(expectAny: false);
        }

        public async Task Step1_GrantIndirectAsync()
        {
            var req = GrantIndirectEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Subject)
                .WithContext(new TargetIdentifier() { Type = TargetIdentifierType.Nip, Value = _f.Target })                
                .WithPermissions(StandardPermissionType.InvoiceRead,
                    StandardPermissionType.InvoiceWrite)
                .WithDescription("E2E indirect grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionIndirectEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchIndirectAsync(bool expectAny)
        {
            var rsp = await kSeFClient
                .SearchEntityAuthorizationGrantsAsync(new Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp = await kSeFClient
                .SearchSubunitAdminPermissionsAsync(new Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

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

        public async Task Step3_RevokeIndirectAsync()
        {

            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await kSeFClient.RevokeAuthorizationsPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.True(string.IsNullOrEmpty(resp.OperationReferenceNumber));
                _f.RevokeResponse.Add(resp);
            }            
        }

        public async Task Step4_SearchIndirectAsync(bool expectAny)
            => await Step2_SearchIndirectAsync(expectAny);
    }
}
