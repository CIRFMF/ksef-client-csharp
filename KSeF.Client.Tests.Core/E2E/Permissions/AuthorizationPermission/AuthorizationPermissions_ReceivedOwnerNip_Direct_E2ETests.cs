using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermissions;

public class AuthorizationPermissions_ReceivedOwnerNip_E2ETests : TestBase
{
    /// <summary>
    /// E2E bez buildera: GRANT (podmiot nadający) → oczekiwanie na 200 → SEARCH Received (NIP właściciela) → asercje → REVOKE → oczekiwanie na 200.
    /// </summary>
    [Fact]
    public async Task Search_Received_AsOwnerNip_Direct_FullFlow_ShouldFindGrantedPermission()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse grantorAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService);
        string grantorAccessToken = grantorAuth.AccessToken.Token;

        // Nadajemy uprawnienie Authorization(SelfInvoicing) podmiotowi o wskazanym NIP (grantor context).
        GrantPermissionsAuthorizationRequest grantRequest =
            GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new AuthorizationSubjectIdentifier
                {
                    Type = AuthorizationSubjectIdentifierType.Nip,
                    Value = ownerNip
                })
                .WithPermission(AuthorizationPermissionType.SelfInvoicing)
                .WithDescription($"E2E-Received-Direct-{ownerNip}")
                .Build();

        OperationResponse grantOperation =
            await KsefClient.GrantsAuthorizationPermissionAsync(grantRequest, grantorAccessToken, CancellationToken);

        // Czekamy aż operacja GRANT osiągnie status 200.
        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, grantorAccessToken),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnienia (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);
        #endregion

        #region Act
        // Dla Received wyszukujemy „po stronie właściciela” (owner context).
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        EntityAuthorizationsQueryRequest searchRequest = new EntityAuthorizationsQueryRequest
        {
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
            {                
                Type = EntityAuthorizationsAuthorizedEntityIdentifierType.Nip,
                Value = ownerNip
            },
            QueryType = QueryType.Received
        };

        // Czekamy aż lista Received zawiera nowo nadane uprawnienie.
        PagedAuthorizationsResponse<AuthorizationGrant> receivedPage =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchEntityAuthorizationGrantsAsync(
                    searchRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                page => page.AuthorizationGrants != null && page.AuthorizationGrants.Any(),
                description: "Czekam aż Received zawiera nowy grant",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        AuthorizationGrant? matching =
            receivedPage.AuthorizationGrants.FirstOrDefault(g =>
                g.AuthorizedEntityIdentifier != null &&
                g.AuthorizedEntityIdentifier.Value == ownerNip &&
                string.Equals(g.AuthorizationScope, AuthorizationPermissionType.SelfInvoicing.ToString(), StringComparison.OrdinalIgnoreCase));
        #endregion

        #region Assert
        Assert.NotNull(receivedPage);
        Assert.NotNull(receivedPage.AuthorizationGrants);
        Assert.NotNull(matching);
        #endregion

        #region Cleanup
        // Unieważnienie (REVOKE) wykonuje grantor.
        OperationResponse revokeOperation =
            await KsefClient.RevokeAuthorizationsPermissionAsync(matching!.Id, grantorAccessToken, CancellationToken);

        PermissionsOperationStatusResponse revokeStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, grantorAccessToken),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na zakończenie REVOKE (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);
        #endregion
    }

    /// <summary>
    /// E2E z builderem zapytania Received: GRANT → 200 → SEARCH(Builder) → asercje → REVOKE → 200.
    /// </summary>
    [Fact]
    public async Task Search_Received_AsOwnerNip_Builder_FullFlow_ShouldFindGrantedPermission()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse grantorAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService);
        string grantorAccessToken = grantorAuth.AccessToken.Token;

        GrantPermissionsAuthorizationRequest grantRequest =
            GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new AuthorizationSubjectIdentifier
                {
                    Type = AuthorizationSubjectIdentifierType.Nip,
                    Value = ownerNip
                })
                .WithPermission(AuthorizationPermissionType.SelfInvoicing)
                .WithDescription($"E2E-Received-Builder-{ownerNip}")
                .Build();

        OperationResponse grantOperation =
            await KsefClient.GrantsAuthorizationPermissionAsync(grantRequest, grantorAccessToken, CancellationToken);

        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, grantorAccessToken),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnienia (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);
        #endregion

        #region Act
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        EntityAuthorizationsQueryRequest searchRequest =
            EntityAuthorizationsQueryRequestBuilder
                .Create()
                .ReceivedForOwnerNip(ownerNip)
                .Build();

        PagedAuthorizationsResponse<AuthorizationGrant> receivedPage =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchEntityAuthorizationGrantsAsync(
                    searchRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                page => page.AuthorizationGrants != null && page.AuthorizationGrants.Any(),
                description: "Czekam aż Received zawiera nowy grant",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        AuthorizationGrant? matching =
            receivedPage.AuthorizationGrants.FirstOrDefault(g =>
                g.AuthorizedEntityIdentifier != null &&
                g.AuthorizedEntityIdentifier.Value == ownerNip &&
                string.Equals(g.AuthorizationScope, AuthorizationPermissionType.SelfInvoicing.ToString(), StringComparison.OrdinalIgnoreCase));
        #endregion

        #region Assert
        Assert.NotNull(receivedPage);
        Assert.NotNull(receivedPage.AuthorizationGrants);
        Assert.NotNull(matching);
        #endregion

        #region Cleanup
        OperationResponse revokeOperation =
            await KsefClient.RevokeAuthorizationsPermissionAsync(matching!.Id, grantorAccessToken, CancellationToken);

        PermissionsOperationStatusResponse revokeStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, grantorAccessToken),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na zakończenie REVOKE (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);
        #endregion
    }
}
