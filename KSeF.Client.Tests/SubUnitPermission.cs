using KSeF.Client.Api.Builders.SubUnitPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.SubUnit;

namespace KSeF.Client.Tests
{
    public class SubUnitPermissionScenarioFixture
    {
        public string AccessToken { get; set; }
        public ContextIdentifier Unit { get; } = new ContextIdentifier
        {
            Type = ContextIdentifierType.Nip,
            Value = "0000000000"
        };

        public SubjectIdentifier SubUnit { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };

        public OperationResponse GrantResponse { get; set; }
        public OperationResponse RevokeResponse { get; set; }
    }

    [CollectionDefinition("SubUnitPermissionScenario")]
    public class SubUnitPermissionScenarioCollection
        : ICollectionFixture<SubUnitPermissionScenarioFixture>
    { }

    [Collection("SubUnitPermissionScenario")]
    public class SubUnitPermissionE2ETests : TestBase
    {
        private readonly SubUnitPermissionScenarioFixture _f;

        public SubUnitPermissionE2ETests(SubUnitPermissionScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Unit.Value = NIP;
            
            _f.SubUnit.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString() + "00";
        }

        [Fact]
        public async Task SubUnitPermission_E2E_GrantAndRevoke()
        {
            // 1. Nadaj uprawnienia sub-jednostce
            await Step1_GrantSubUnitPermissionsAsync();
            Thread.Sleep(sleepTime);

            // 2. Cofnij uprawnienia sub-jednostce
            await Step2_RevokeSubUnitPermissionsAsync();
        }

        public async Task Step1_GrantSubUnitPermissionsAsync()
        {
            var req = GrantSubUnitPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.SubUnit)
                .WithContext(_f.Unit)
                .WithDescription("E2E test grant sub-unit")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionSubUnitAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            _f.GrantResponse = resp;
        }

        public async Task Step2_RevokeSubUnitPermissionsAsync()
        {
            var resp = await kSeFClient
                .RevokeAuthorizationsPermissionAsync(string.Empty, _f.AccessToken, CancellationToken.None);//TODO permissionId

            Assert.NotNull(resp);
            _f.RevokeResponse = resp;
        }
    }
}
