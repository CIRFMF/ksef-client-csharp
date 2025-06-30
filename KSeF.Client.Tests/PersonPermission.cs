using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;

namespace KSeF.Client.Tests
{
    public class PersonPermissionScenarioFixture
    {
        public string AccessToken { get; set; }

        // Testowy użytkownik — tu wstaw realny identyfikator (NIP lub PESEL)
        public SubjectIdentifier Person { get; } =
            new SubjectIdentifier {Value = "0000000000" ,
                Type = SubjectIdentifierType.Pesel};

        public OperationResponse GrantResponse { get; set; }
        public OperationResponse RevokeResponse { get; set; }
        public PagedPermissionsResponse<PersonPermission> SearchResponse { get; set; }
    }

    [CollectionDefinition("PersonPermissionScenario")]
    public class PersonPermissionScenarioCollection
        : ICollectionFixture<PersonPermissionScenarioFixture>
    { }

    [Collection("PersonPermissionScenario")]
    public class PersonPermissionE2ETests : TestBase
    {
        private readonly PersonPermissionScenarioFixture _f;

        public PersonPermissionE2ETests(PersonPermissionScenarioFixture f)
        {
            _f = f;
            _f.Person.Value = NIP;
            _f.AccessToken = AccessToken;
            _f.Person.Value = randomGenerator.Next(900000000, 999999999).ToString() + "00";
        }

        [Fact]
        public async Task PersonPermission_E2E_GrantSearchRevokeSearch()
        {
            // 1. Nadaj uprawnienia
            await Step1_GrantPermissionsAsync();
            Thread.Sleep(sleepTime);
            // 2. Wyszukaj — powinny się pojawić
            await Step2_SearchGrantedPermissionsAsync(expectAny: true);
            Thread.Sleep(sleepTime);
            // 3. Cofnij uprawnienia
            await Step3_RevokePermissionsAsync();
            Thread.Sleep(sleepTime);
            // 4. Wyszukaj ponownie — nie powinno być wpisów
            await Step4_SearchGrantedPermissionsAsync(expectAny: false);
        }

        public async Task Step1_GrantPermissionsAsync()
        {
            var req = GrantPersonPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Person)
                .WithPermissions(
                    StandardPermissionType.InvoiceRead,
                    StandardPermissionType.InvoiceWrite)
                .WithDescription("E2E test grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionPersonAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.True(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchGrantedPermissionsAsync(bool expectAny)
        {
            var query = new PersonPermissionsQueryRequest
            {
                PermissionTypes = new List<PersonPermissionType>
                {
                    PersonPermissionType.InvoiceRead,
                    PersonPermissionType.InvoiceWrite
                }
            };

            var resp = await kSeFClient
                .SearchGrantedPersonPermissionsAsync(
                    query,
                    _f.AccessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken.None);

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

        public async Task Step3_RevokePermissionsAsync()
        {
            string permissionId = string.Empty;

            var resp = await kSeFClient
                .RevokeCommonPermissionAsync(permissionId, _f.AccessToken, CancellationToken.None);


            Assert.NotNull(resp);
            Assert.True(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.RevokeResponse = resp;
        }

        public async Task Step4_SearchGrantedPermissionsAsync(bool expectAny)
        {
            await Step2_SearchGrantedPermissionsAsync(expectAny);
        }
    }
}
