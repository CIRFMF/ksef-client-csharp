#nullable enable
using KSeF.Client.Api.Builders.AuthorizationEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermission;

public class AuthorizationPermissions_ReceivedOwnerNip_E2ETests : TestBase
{
    /// <summary>
    /// Nadanie uprawnień → wyszukanie uprawnień (Otrzymanie, NIP właściciela) → odebranie uprawnień.
    /// </summary>
    [Fact]
    public async Task Search_Received_AsOwnerNip_Direct_FullFlow_ShouldFindGrantedPermission()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse grantorAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient);
        string grantorAccessToken = grantorAuth.AccessToken.Token;

        PermissionsAuthorizationSubjectDetails subjectDetails = new PermissionsAuthorizationSubjectDetails
        {
            FullName = $"Właściciel {ownerNip}"
        };
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
                .WithSubjectDetails(subjectDetails)
                .Build();

        OperationResponse grantOperation =
            await KsefClient.GrantsAuthorizationPermissionAsync(grantRequest, grantorAccessToken, CancellationToken);

        // Czekamy aż operacja GRANT osiągnie status 200.
        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, grantorAccessToken).ConfigureAwait(false),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnienia (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                cancellationToken: CancellationToken);
        #endregion

        #region Act
        // Dla sekcji „Otrzymane” wyszukujemy „po stronie właściciela” (kontekst właściciela).
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        EntityAuthorizationsQueryRequest searchRequest = new()
        {
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
            {                
                Type = EntityAuthorizationsAuthorizedEntityIdentifierType.Nip,
                Value = ownerNip
            },
            QueryType = QueryType.Received
        };

        PagedAuthorizationsResponse<AuthorizationGrant> receivedPage =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchEntityAuthorizationGrantsAsync(
                    searchRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken).ConfigureAwait(false),
                page => page.AuthorizationGrants != null && page.AuthorizationGrants.Count > 0,
                description: "Oczekiwanie na nowo nadane uprawnienia",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                cancellationToken: CancellationToken);

        AuthorizationGrant? matching =
            receivedPage.AuthorizationGrants.FirstOrDefault(g =>
                g.AuthorizedEntityIdentifier != null &&
                g.AuthorizedEntityIdentifier.Value == ownerNip &&
                g.AuthorizationScope == AuthorizationPermissionType.SelfInvoicing &&
                g.SubjectEntityDetails.FullName == subjectDetails.FullName);
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
                async () => await KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, grantorAccessToken).ConfigureAwait(false),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Oczekiwanie na zakończenie odbierania uprawnień",
                cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);
        #endregion
    }

    /// <summary>
    /// E2E z builderem zapytania „Otrzymane”: GRANT → 200 → SEARCH(Builder) → asercje → REVOKE → 200.
    /// </summary>
    [Fact]
    public async Task Search_Received_AsOwnerNip_Builder_FullFlow_ShouldFindGrantedPermission()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse grantorAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient);
        string grantorAccessToken = grantorAuth.AccessToken.Token;
        PermissionsAuthorizationSubjectDetails subjectDetails = new PermissionsAuthorizationSubjectDetails
        {
            FullName = $"Właściciel {ownerNip}"
        };
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
                .WithSubjectDetails(subjectDetails)
                .Build();

        OperationResponse grantOperation =
            await KsefClient.GrantsAuthorizationPermissionAsync(grantRequest, grantorAccessToken, CancellationToken);

        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, grantorAccessToken).ConfigureAwait(false),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnienia (200)",
                cancellationToken: CancellationToken);
        #endregion

        #region Act
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        EntityAuthorizationsQueryRequest searchRequest =
            EntityAuthorizationsQueryRequestBuilder
                .Create()
                .ReceivedForOwnerNip(ownerNip)
                .Build();

        PagedAuthorizationsResponse<AuthorizationGrant> receivedPage =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchEntityAuthorizationGrantsAsync(
                    searchRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken).ConfigureAwait(false),
                page => page.AuthorizationGrants != null && page.AuthorizationGrants.Count > 0,
                cancellationToken: CancellationToken);

        AuthorizationGrant? matching =
            receivedPage.AuthorizationGrants.FirstOrDefault(g =>
                g.AuthorizedEntityIdentifier != null &&
                g.AuthorizedEntityIdentifier.Value == ownerNip &&
                g.AuthorizationScope == AuthorizationPermissionType.SelfInvoicing &&
                g.SubjectEntityDetails.FullName == subjectDetails.FullName);
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
                async () => await KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, grantorAccessToken).ConfigureAwait(false),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);
        #endregion
    }
}
