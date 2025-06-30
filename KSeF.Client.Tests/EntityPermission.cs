using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;

namespace KSeF.Client.Tests
{
    public class EntityPermissionScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier Entity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };

        public OperationResponse GrantResponse { get; set; }
        public IList<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        public PagedPermissionsResponse<PersonPermission> SearchResponse { get; internal set; }
    }

    [CollectionDefinition("EntityPermissionScenario")]
    public class EntityPermissionScenarioCollection
        : ICollectionFixture<EntityPermissionScenarioFixture>
    { }

    [Collection("EntityPermissionScenario")]
    public class EntityPermissionE2ETests : TestBase
    {
        private readonly EntityPermissionScenarioFixture _f;

        public EntityPermissionE2ETests(EntityPermissionScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Entity.Value = NIP;
            _f.Entity.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString() + "00";
        }

        [Fact]
        public async Task EntityPermission_E2E_GrantSearchRevokeSearch()
        {
            // 1. Nadaj uprawnienia
            await Step1_GrantPermissionsAsync();
            Thread.Sleep(sleepTime);

            // 2. Wyszukaj — powinny się pojawić
            await Step2_SearchGrantedRolesAsync(expectAny: true);
            Thread.Sleep(sleepTime);

            // 3. Cofnij uprawnienia
            await Step3_RevokePermissionsAsync();
            Thread.Sleep(sleepTime);

            // 4. Wyszukaj ponownie — nie powinno być wpisów
            await Step4_SearchGrantedPermissionsAsync(expectAny: false);
        }

        public async Task Step1_GrantPermissionsAsync()
        {
            var req = GrantEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Entity)
                .WithPermissions(
                    Permission.New(StandardPermissionType.InvoiceRead, true),
                    Permission.New(StandardPermissionType.InvoiceWrite, false)                   
                    )
                .WithDescription("E2E test grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchGrantedRolesAsync(bool expectAny)
        {
            var resp = await kSeFClient
            .SearchGrantedPersonPermissionsAsync(
              new Core.Models.Permissions.Person.PersonPermissionsQueryRequest(),
              _f.AccessToken
            );

            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.NotEmpty(resp.Permissions);
                Assert.True(resp.Permissions.Count > 0);
                Assert.True(resp.Permissions.All(x=> x.Description == "E2E test grant"));
                Assert.True(resp.Permissions.First(x=> x.CanDelegate == true &&  Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead) is not null);
                Assert.True(resp.Permissions.First(x=> x.CanDelegate == false &&  Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite) is not null);
                _f.SearchResponse = resp;
            }
            else
            {
                Assert.Empty(resp.Permissions);
            }
        }

        public async Task Step3_RevokePermissionsAsync()
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

        public async Task Step4_SearchGrantedPermissionsAsync(bool expectAny)
            => await Step2_SearchGrantedRolesAsync(expectAny);
    }
}
