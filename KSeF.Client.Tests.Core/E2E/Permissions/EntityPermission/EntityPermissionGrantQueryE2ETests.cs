using KSeF.Client.Api.Builders.SubEntityPermissions;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission
{
    public class EntityPermissionGrantQueryE2ETests() : TestBase
    {
        [Fact]
        public async Task QueryEntitiesGrants_ReturnsPermissions()
        {
            ISearchPermissionClient searchPermissionClient = KsefClient;

            string nip = MiscellaneousUtils.GetRandomNip();

            AuthenticationOperationStatusResponse authOperationStatusResponse = 
                await AuthenticationUtils.AuthenticateAsync(AuthorizationClient,nip,nip);         

            EntityPermissionGrantQueryRequest request = new EntityPermissionGrantQueryRequest
            {
                ContextIdentifier = new EntityPermissionGrantQueryContextIdentifier
                {
                    Type = EntityPermissionGrantQueryContextIdentifierType.Nip,
                    Value = nip
                }
            };
            
            EntityPermissionGrantResponse queryEntitiesGrantsAsyncResponse = 
                await searchPermissionClient.QueryEntitiesGrantsAsync(request, authOperationStatusResponse.AccessToken.Token);

            Assert.NotNull(queryEntitiesGrantsAsyncResponse);
            Assert.NotNull(queryEntitiesGrantsAsyncResponse.Permissions);
            Assert.True(queryEntitiesGrantsAsyncResponse.Permissions.Count >= 0);
        }

        [Fact]
        public async Task QueryEntitiesGrants_WithInternalId_ReturnsPermissions()
        {

            ISearchPermissionClient searchPermissionClient = KsefClient;

            string nip = MiscellaneousUtils.GetRandomNip();
            string internalId = MiscellaneousUtils.GenerateInternalIdentifier(nip);

            AuthenticationOperationStatusResponse authOperationStatusResponse =
                await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip, nip);

            GrantPermissionsSubunitRequest grantSubunitPermissionsReques = GrantSubunitPermissionsRequestBuilder
            .Create()
            .WithSubject(new SubunitSubjectIdentifier
            {
                Type = SubUnitSubjectIdentifierType.Nip,
                Value = nip
            })
            .WithContext(new SubunitContextIdentifier
            {
                Type = SubunitContextIdentifierType.InternalId,
                Value = internalId
            })
            .WithSubunitName("E2E VATGroup Subunit")
            .WithDescription("E2E - grant subunit admin by subunit context").WithSubjectDetails(new SubunitSubjectDetails
            {
                SubjectDetailsType = PermissionsSubunitSubjectDetailsType.PersonByIdentifier,
                PersonById = new PermissionsSubunitPersonByIdentifier { FirstName = "Jan", LastName = "Kowalski" }
            })
            .Build();

            await KsefClient.GrantsPermissionSubUnitAsync(grantSubunitPermissionsReques, authOperationStatusResponse.AccessToken.Token, CancellationToken).ConfigureAwait(false);


            AuthenticationOperationStatusResponse internalAuthOperationStatusResponse =
                await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextIdentifierType: AuthenticationTokenContextIdentifierType.InternalId, identifier: internalId);

            EntityPermissionGrantQueryRequest request = new EntityPermissionGrantQueryRequest
            {
                ContextIdentifier = new EntityPermissionGrantQueryContextIdentifier
                {
                    Type = EntityPermissionGrantQueryContextIdentifierType.InternalId,
                    Value = internalId
                }
            };

            EntityPermissionGrantResponse queryEntitiesGrantsAsyncResponse =
                await searchPermissionClient.QueryEntitiesGrantsAsync(request, authOperationStatusResponse.AccessToken.Token);

            Assert.NotNull(queryEntitiesGrantsAsyncResponse);
            Assert.NotNull(queryEntitiesGrantsAsyncResponse.Permissions);
            Assert.True(queryEntitiesGrantsAsyncResponse.Permissions.Count >= 0);
        }
    }
}
