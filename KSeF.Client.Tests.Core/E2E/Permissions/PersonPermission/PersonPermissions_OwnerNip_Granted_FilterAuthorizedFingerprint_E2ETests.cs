using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonPermissions_OwnerNip_Granted_FilterAuthorizedFingerprint_E2ETests : TestBase
{

    /// <summary>
    /// E2E: nadane uprawnienia (właściciel) w kontekście NIP z filtrowaniem po odcisku palca certyfikatu (fingerprint SHA-256).
    /// </summary>
    /// <remarks>  
    /// <list type="number">
    /// <item><description>Właściciel podmiotu – pełen dostęp w kontekście własnego NIP; powiązanie NIP–PESEL.</description></item>
    /// <item><description>Generujemy cert testowy → fingerprint (SHA-256, HEX, UPPER).</description></item>
    /// <item><description>GRANT dla fingerprint → poll (200) → QUERY z filtrem fingerprint.</description></item>
    /// <item><description>Asercja dopasowania fingerprint.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task Search_Granted_AsOwnerNip_FilterByAuthorizedFingerprint_ShouldReturnMatch()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        // cert testowy → fingerprint (SHA256 HEX, uppercase)
        X509Certificate2 personCert = CertificateUtils.GetPersonalCertificate(
            givenName: "PL",
            surname: "Person",
            serialNumberPrefix: "TINPL",
            serialNumber: ownerNip,
            commonName: "E2E Authorized Person");
        string authorizedFingerprint = CertificateUtils.GetSha256Fingerprint(personCert);

        // owner (nadawca == owner)
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        // GRANT → nadaj np. InvoiceRead dla fingerprintu
        GrantPermissionsPersonRequest grantRequest = new GrantPermissionsPersonRequest
        {
            SubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Fingerprint,
                Value = authorizedFingerprint
            },
            Permissions = new PersonPermissionType[]
            {
                PersonPermissionType.InvoiceRead
            },
            Description = $"E2E-Grant-Read-FP-{authorizedFingerprint[..8]}"
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

        // Zapytanie: nadane uprawnienia (owner) z filtrem po Fingerprint
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
                Type = PersonAuthorizedIdentifierType.Fingerprint,
                Value = authorizedFingerprint
            },
            PermissionState = PersonPermissionState.Active,
            QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
        };
        #endregion

        #region Act
        // Polling aż pojawi się wpis Z TYM fingerprintem
        PagedPermissionsResponse<PersonPermission> page =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonPermissionsAsync(
                    queryRequest, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                condition: r => r.Permissions != null && r.Permissions.Any(p =>
                        p?.AuthorizedIdentifier != null &&
                        p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Fingerprint &&
                        string.Equals(p.AuthorizedIdentifier.Value, authorizedFingerprint, StringComparison.Ordinal)),
                description: "Czekam aż pojawi się wpis (nadane / fingerprint)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        PersonPermission? matching = page.Permissions.FirstOrDefault(p =>
            p is not null
            && p.AuthorizedIdentifier is not null
            && p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Fingerprint
            && string.Equals(p.AuthorizedIdentifier.Value, authorizedFingerprint, StringComparison.Ordinal));
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
