using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonPermissions_OwnerNip_Granted_FilterAuthorizedPesel_E2ETests : TestBase
{

    /// <summary>
    /// E2E: nadane uprawnienia (właściciel) w kontekście NIP z filtrowaniem po PESEL.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Nadanie uprawnienia osobie z PESEL → poll (200).</description></item>
    /// <item><description>Zapytanie: nadane w bieżącym kontekście + filtr PESEL.</description></item>
    /// <item><description>Asercja: istnieje wpis z dokładnie tym PESEL.</description></item>
    /// <item><description>Sprzątanie: odebranie uprawnienia (200).</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task Search_Granted_AsOwnerNip_FilterByAuthorizedPesel_ShouldReturnMatch()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string authorizedPesel = MiscellaneousUtils.GetRandomPesel();

        // owner (nadawca == owner)
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        // GRANT — nadajemy np. InvoiceRead osobie o PESEL
        GrantPermissionsPersonRequest grantRequest = new GrantPermissionsPersonRequest
        {
            SubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
                Value = authorizedPesel
            },
            Permissions = new PersonPermissionType[]
            {
                PersonPermissionType.InvoiceRead
            },
            Description = $"E2E-Grant-Read-PESEL-{authorizedPesel}"
        };

        OperationResponse grantOperation =
            await KsefClient.GrantsPermissionPersonAsync(grantRequest, ownerAccessToken, CancellationToken);

        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, ownerAccessToken),
                condition: r => r.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnienia (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        // Query: nadane (owner) + filtr po PESEL
        PersonPermissionsQueryRequest queryRequest = new PersonPermissionsQueryRequest
        {
            ContextIdentifier = new PersonPermissionsContextIdentifier
            {
                Type = PersonPermissionsContextIdentifierType.Nip,
                Value = ownerNip
            },
            TargetIdentifier = new PersonPermissionsTargetIdentifier
            {
                Type = PersonPermissionsTargetIdentifierType.Nip,
                Value = ownerNip
            },
            AuthorizedIdentifier = new PersonPermissionsAuthorizedIdentifier
            {
                Type = PersonAuthorizedIdentifierType.Pesel,
                Value = authorizedPesel
            },
            PermissionState = PersonPermissionState.Active,
            QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
        };
        #endregion

        #region Act
        PagedPermissionsResponse<PersonPermission> page =
            await AsyncPollingUtils.PollAsync(
                () => KsefClient.SearchGrantedPersonPermissionsAsync(
                    queryRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                r => r.Permissions != null && r.Permissions.Count > 0,
                description: "Czekam aż pojawi się wpis (nadane / PESEL)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        PersonPermission? matching = page.Permissions.FirstOrDefault(p =>
            p is not null
            && p.AuthorizedIdentifier is not null
            && p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Pesel
            && string.Equals(p.AuthorizedIdentifier.Value, authorizedPesel, StringComparison.Ordinal));
        #endregion

        #region Assert
        Assert.NotNull(page);
        Assert.NotNull(page.Permissions);
        Assert.NotNull(matching);
        #endregion

        #region Cleanup
        // REVOKE po Id uprawnienia
        OperationResponse revokeOperation =
            await KsefClient.RevokeCommonPermissionAsync(matching!.Id, ownerAccessToken, CancellationToken);

        PermissionsOperationStatusResponse revokeStatus =
            await AsyncPollingUtils.PollAsync(
                () => KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, ownerAccessToken),
                r => r.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na zakończenie REVOKE (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);
        #endregion
    }
}
