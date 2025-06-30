using KSeF.Client.Api.Builders.ProxyEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.ProxyEntity;

namespace KSeF.Client.Tests
{
    public class ProxyPermissionsScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier Entity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };

        public OperationResponse GrantResponse { get; set; }
        public OperationResponse RevokeResponse { get; set; }
        public PagedRolesResponse<EntityRole> SearchResponse { get; set; }
    }

    [CollectionDefinition("ProxyPermissionsScenario")]
    public class ProxyPermissionsScenarioCollection
        : ICollectionFixture<ProxyPermissionsScenarioFixture>
    { }

    [Collection("ProxyPermissionsScenario")]
    public class ProxyPermissionsE2ETests : TestBase
    {
        private readonly ProxyPermissionsScenarioFixture _f;

        public ProxyPermissionsE2ETests(ProxyPermissionsScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Entity.Value = NIP;
            _f.Entity.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString() + "00";
        }

        [Fact]
        public async Task ProxyPermissions_E2E_GrantSearchRevokeSearch()
        {
            // 1. Nadaj uprawnienia
            await Step1_GrantPermissionsAsync();
            Thread.Sleep(sleepTime);

            // 2. Wyszukaj — powinny się pojawić
            //TODO ustalić czy coś powinno się pojawić, ewentualnie użyć endpointa dedykowanego jeśli zostanie zaimplementowany
            await Step2_SearchGrantedRolesAsync(expectAny: false);
            Thread.Sleep(sleepTime);

            // 3. Cofnij uprawnienia
            await Step3_RevokePermissionsAsync();
            Thread.Sleep(sleepTime);

            // 4. Wyszukaj ponownie — nie powinno być wpisów
            await Step4_SearchGrantedPermissionsAsync(expectAny: false);
        }

        public async Task Step1_GrantPermissionsAsync()
        {
            var req = GrantProxyEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Entity)
                .WithPermission(StandardPermissionType.SelfInvoicing)
                .WithDescription("E2E test grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionProxyEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchGrantedRolesAsync(bool expectAny)
        {
            var resp = await kSeFClient
                .SearchEntityInvoiceRolesAsync(
                    _f.AccessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken.None
                );

            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.NotEmpty(resp.Roles);
                _f.SearchResponse = resp;
            }
            else
            {
                Assert.Empty(resp.Roles);
            }
        }

        public async Task Step3_RevokePermissionsAsync()
        {
            string permissionId = null;
            var resp = await kSeFClient
                .RevokeAuthorizationsPermissionAsync(permissionId, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.RevokeResponse = resp;
        }

        public async Task Step4_SearchGrantedPermissionsAsync(bool expectAny)
            => await Step2_SearchGrantedRolesAsync(expectAny);
    }
}
