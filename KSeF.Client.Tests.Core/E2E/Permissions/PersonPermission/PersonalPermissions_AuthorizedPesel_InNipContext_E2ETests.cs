using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;
using static KSeF.Client.Core.Models.Permissions.Identifiers.PersonalPermissionsTargetIdentifier;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonalPermissions_AuthorizedPesel_InNipContext_E2ETests : TestBase
{

    /// <summary>
    /// Pobranie listy obowiązujących uprawnień do pracy w KSeF jako osoba uprawniona PESEL w kontekście NIP (E2E).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///   <item><description>Subject (NIP właściciela) – utworzenie (testdata).</description></item>
    ///   <item><description>Uwierzytelnienie właściciela (kontekst NIP) → nadanie uprawnień (PESEL) przez publiczne API.</description></item>
    ///   <item><description>Uwierzytelnienie osoby (PESEL) certyfikatem w kontekście NIP właściciela.</description></item>
    ///   <item><description>Zapytanie: personal/grants, stan = Active, cel = NIP; asercje.</description></item>
    ///   <item><description>Sprzątanie: odebranie uprawnień (publiczne API) + usunięcie subject (testdata).</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task Search_MyActive_InNipContext_AsAuthorizedPesel_ShouldReturnGrantedPermissions()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string authorizedPesel = MiscellaneousUtils.GetRandomPesel();
        string description = $"E2E-MyActive-PeselInNip-{authorizedPesel}";

        // Subject (kontekst właściciela) – testdata setup
        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest
        {
            SubjectNip = ownerNip,
            Description = $"E2E-Subject-{ownerNip}"
        }, CancellationToken);

        // Owner auth (kontekst NIP)
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        // GRANT (publiczne API): nadajemy InvoiceRead + InvoiceWrite dla osoby identyfikowanej PESEL
        GrantPermissionsPersonRequest grantRequest = new GrantPermissionsPersonRequest
        {
            SubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
                Value = authorizedPesel
            },
            Permissions = new PersonPermissionType[]
            {
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite
            },
            Description = description
        };

        OperationResponse grantOperation =
            await KsefClient.GrantsPermissionPersonAsync(grantRequest, ownerAccessToken, CancellationToken);

        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, ownerAccessToken),
                result => result is not null && result.Status is not null && result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam na nadanie uprawnień (200)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Osoba (PESEL) – generujemy cert testowy i uwierzytelniamy się w KONTEKŚCIE NIP właściciela
        using System.Security.Cryptography.X509Certificates.X509Certificate2 personalCertificate =
            SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName("A")
                .WithSurname("R")
                .WithSerialNumber("PNOPL-" + authorizedPesel)
                .WithCommonName("Authorized Person")
                .Build();

        AuthenticationOperationStatusResponse personAuth =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                ownerNip,
                AuthenticationTokenContextIdentifierType.Nip,
                personalCertificate);

        string personAccessToken = personAuth.AccessToken.Token;

        // Zapytanie: personal/grants — obowiązujące (Active) uprawnienia w bieżącym kontekście NIP
        PersonalPermissionsQueryRequest query = new PersonalPermissionsQueryRequest
        {
            ContextIdentifier = new PersonalPermissionsContextIdentifier
            {
                Type = PersonalPermissionsContextIdentifierType.Nip,
                Value = ownerNip
            },
            TargetIdentifier = new PersonalPermissionsTargetIdentifier
            {
                Type = PersonalPermissionsTargetIdentifierType.Nip,
                Value = ownerNip
            },
            PermissionState = PersonPermissionState.Active
        };
        #endregion

        #region Act
        PagedPermissionsResponse<PersonalPermission> page =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchGrantedPersonalPermissionsAsync(
                    query, personAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                result => result is not null
                          && result.Permissions is not null
                          && result.Permissions.Any(p => p.Description == description
                                                      && p.PermissionState == PersonalPermissionState.Active),
                description: "Czekam aż pojawi się wpis (obowiązujące / kontekst NIP)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // wyłuskaj obie nadane w tym teście (po opisie)
        PersonalPermission[] inContext = page.Permissions
            .Where(p => p.Description == description && p.PermissionState == PersonalPermissionState.Active)
            .ToArray();
        #endregion

        #region Assert
        Assert.NotNull(page);
        Assert.NotNull(page.Permissions);
        Assert.Equal(2, inContext.Length);
        Assert.Contains(inContext, p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead);
        Assert.Contains(inContext, p => p.PermissionScope == PersonalPermissionScopeType.InvoiceWrite);
        #endregion

        #region Cleanup
        // REVOKE (publiczne API) – po Id każdej pozycji z tego testu
        foreach (PersonalPermission permission in inContext)
        {
            OperationResponse revokeOp =
                await KsefClient.RevokeCommonPermissionAsync(permission.Id, ownerAccessToken, CancellationToken);

            PermissionsOperationStatusResponse revokeStatus =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeOp.ReferenceNumber, ownerAccessToken),
                    result => result is not null && result.Status is not null && result.Status.Code == OperationStatusCodeResponse.Success,
                    description: "Czekam na odebranie uprawnień (200)",
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken);

            Assert.NotNull(revokeStatus);
        }

        // subject cleanup
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest
        {
            SubjectNip = ownerNip
        }, CancellationToken);
        #endregion
    }
}
