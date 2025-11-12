using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [Collection("CredentialsGrantScenario")]
    [Trait("Category", "Features")]
    [Trait("Features", "credentials_grant.feature")]
    public class CredentialsGrantTests : KsefIntegrationTestBase
    {
        [Theory]
        [InlineData("90091309123", new[] { PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead, PersonPermissionType.Introspection, PersonPermissionType.CredentialsRead, PersonPermissionType.CredentialsManage })]
        [InlineData("6651887777", new[] { PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead, PersonPermissionType.Introspection, PersonPermissionType.CredentialsRead, PersonPermissionType.CredentialsManage })]
        [Trait("Scenario", "Nadanie uprawnienia wystawianie faktur")]
        public async Task GivenOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identyficator, PersonPermissionType[] permissions)
        {
            string ownerNip = MiscellaneousUtils.GetRandomNip();

            string authToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

            bool isNIP = identyficator.Length == 10; //zmienna identifier obsługuje dwa typy podmiotów. nr. NIP oraz nr. PESEL. Zmienne rozróżniane są po długości.

            GrantPermissionsPersonSubjectIdentifier subjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = isNIP ? GrantPermissionsPersonSubjectIdentifierType.Nip : GrantPermissionsPersonSubjectIdentifierType.Pesel, Value = identyficator };

            OperationResponse grantPermissionsResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient,
                    authToken,
                    subjectIdentifier,
                    permissions, "CredentialsGrantTests");

            PermissionsOperationStatusResponse operationStatus = await AsyncPollingUtils.PollAsync(
                async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, grantPermissionsResponse.ReferenceNumber, authToken),
                status => status is not null &&
                         status.Status is not null &&
                         status.Status.Code == 200,
                delay: TimeSpan.FromSeconds(5),
                maxAttempts: 60,
                cancellationToken: CancellationToken.None);

            IReadOnlyList<PersonPermission> grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.True(grantedPermissions.Count == permissions.Length);

            //uwierzytelnienie w kontekście w którym otrzymano uprawnienia
            Core.Models.Authorization.AuthenticationOperationStatusResponse authContext = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, identyficator, ownerNip);
            Assert.NotNull(authContext);
            PersonToken personToken = TokenService.MapFromJwt(authContext.AccessToken.Token);
            Assert.True(personToken.Permissions.Length == permissions.Length);

            foreach (PersonPermission item in grantedPermissions)
            {
                OperationResponse revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);

                PermissionsOperationStatusResponse revokePermissionsActionStatus = await AsyncPollingUtils.PollAsync(
                    async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, revokeSuccessful.ReferenceNumber, authToken),
                    status => status is not null &&
                             status.Status is not null &&
                             status.Status.Code == 200,
                    delay: TimeSpan.FromSeconds(2),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken.None);
            }

            IReadOnlyList<PersonPermission> activePermissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }

        [Theory]
        [InlineData("90091309123", new[] { PersonPermissionType.InvoiceRead, PersonPermissionType.InvoiceWrite, PersonPermissionType.CredentialsManage,PersonPermissionType.CredentialsRead, PersonPermissionType.Introspection, PersonPermissionType.SubunitManage })]
        [InlineData("6651887777", new[] { PersonPermissionType.InvoiceWrite, PersonPermissionType.InvoiceRead, PersonPermissionType.Introspection, PersonPermissionType.CredentialsRead, PersonPermissionType.CredentialsManage  })]
        [Trait("Scenario", "Nadanie uprawnień przez osobę z uprawnieniem do zarządzania uprawnieniami")]
        public async Task GivenDelegatedByOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identifier, PersonPermissionType[] permissions)
        {
            bool isNip = identifier.Length == 10; //zmienna identifier obsługuje dwa typy podmiotów. nr. NIP oraz nr. PESEL. Zmienne rozróżniane są po długości.
            //Arange
            string ownerNip = MiscellaneousUtils.GetRandomNip();
            string authToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

            string delegateNip = MiscellaneousUtils.GetRandomNip();

            // nadanie uprawnień CredentialsManage
            GrantPermissionsPersonSubjectIdentifier subjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = GrantPermissionsPersonSubjectIdentifierType.Nip, Value = delegateNip };
            PersonPermissionType[] managePermission = new[] { PersonPermissionType.CredentialsManage };
            OperationResponse operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient, authToken, subjectIdentifier, managePermission);

            PermissionsOperationStatusResponse operationStatus = await AsyncPollingUtils.PollAsync(
                async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, operationResponse.ReferenceNumber, authToken),
                status => status is not null &&
                         status.Status is not null &&
                         status.Status.Code == 200,
                delay: TimeSpan.FromSeconds(5),
                maxAttempts: 60,
                cancellationToken: CancellationToken.None);

            //Nadanie uprawnień jako menadżer uprawnień
            string delegateAuthToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, delegateNip, ownerNip)).AccessToken.Token;

            GrantPermissionsPersonSubjectIdentifier grantPermissionsPersonSubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = isNip ? GrantPermissionsPersonSubjectIdentifierType.Nip : GrantPermissionsPersonSubjectIdentifierType.Pesel, Value = identifier };

            OperationResponse grantPermissionsResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient,
                    delegateAuthToken,
                    grantPermissionsPersonSubjectIdentifier,
                    permissions, "CredentialsGrantTests");

            PermissionsOperationStatusResponse grantPermissionsActionStatus = await AsyncPollingUtils.PollAsync(
                async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, operationResponse.ReferenceNumber, delegateAuthToken),
                status => status is not null &&
                         status.Status is not null &&
                         status.Status.Code == 200,
                delay: TimeSpan.FromSeconds(5),
                maxAttempts: 60,
                cancellationToken: CancellationToken.None);

            IReadOnlyList<PersonPermission> grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, delegateAuthToken, PersonPermissionState.Active);
            Assert.True(grantedPermissions.Count(x=> x.AuthorizedIdentifier.Value == identifier) == permissions.Length);

            //uwierzytelnienie w kontekście w którym otrzymano uprawnienia
            Core.Models.Authorization.AuthenticationOperationStatusResponse authContext = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, identifier, ownerNip);
            Assert.NotNull(authContext);
            PersonToken personToken = TokenService.MapFromJwt(authContext.AccessToken.Token);
            Assert.True(personToken.Permissions.Length == permissions.Length);

            foreach (PersonPermission item in grantedPermissions)
            {
                OperationResponse revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);

                PermissionsOperationStatusResponse revokePermissionsActionStatus = await AsyncPollingUtils.PollAsync(
                    async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, revokeSuccessful.ReferenceNumber, authToken),
                    status => status is not null &&
                             status.Status is not null &&
                             status.Status.Code == 200,
                    delay: TimeSpan.FromSeconds(2),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken.None);
            }

            //wyszukanie uprawnień z poziomu delegata
            IReadOnlyList<PersonPermission> activePermissionsFromSubjectAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, delegateAuthToken, PersonPermissionState.Active);
            Assert.Empty(activePermissionsFromSubjectAfterRevoke);
        }
    }
}