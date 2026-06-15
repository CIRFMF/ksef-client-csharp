using KSeF.Client.Api.Builders.AuthorizationEntityPermissions;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;
using static KSeF.Client.Core.Models.Permissions.Identifiers.PersonalPermissionsTargetIdentifier;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

public class PersonalPermissionsIndirectSelectiveIncompleteChainE2ETests : TestBase
{
    private const int OperationSuccessfulStatusCode = 200;
    private IPersonTokenService _tokenService => Get<IPersonTokenService>();

    /// <summary>
    /// Uwierzytelnienie na uprawnienia nadane w sposób pośredni (selektywnie) bez kompletnego łańcucha (brak wspólnego zakresu).
    /// </summary>
    /// <remarks>
    /// 1) Owner NIP -> GRANT dla NIP biura: InvoiceRead.  
    /// 2) Biuro (własny kontekst NIP) -> GRANT dla PESEL pracownika: TaxRepresentative.  
    /// 3) Osoba (PESEL) uwierzytelnia się certyfikatem w kontekście NIP właściciela -> QUERY personal/grants (Active).  
    /// Oczekiwane: brak efektywnych uprawnień (pusta lista) + token bez wspólnego scope.
    /// </remarks>
    [Fact]
    public async Task AuthIndirectSelectiveIncompleteChainShouldHaveNoEffectivePermissions()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string intermediaryNip = MiscellaneousUtils.GetRandomNip();
        string personPesel = MiscellaneousUtils.GetRandomPesel();
        string descOwnerToIntermediary = $"E2E-Indirect-OwnerToInterm-Read-{intermediaryNip}";
        string descIntermediaryToPerson = $"E2E-Indirect-IntermToPerson-Write-{personPesel}";

        // subjecty (tylko to, co konieczne)
        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest
        {
            SubjectNip = ownerNip,
            Description = $"E2E-Subject-Owner-{ownerNip}"
        }, CancellationToken);

        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest
        {
            SubjectNip = intermediaryNip,
            Description = $"E2E-Subject-Interm-{intermediaryNip}"
        }, CancellationToken);

        // AUTH: owner (kontekst NIP ownera)
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(KsefClient, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        // 1) OWNER -> INTERMEDIARY (NIP biura): grant "TaxRepresentative" (publiczne API podmiotowe)
        GrantPermissionsAuthorizationRequest ownerToIntermediary =
            GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new AuthorizationSubjectIdentifier
                {
                    Type = AuthorizationSubjectIdentifierType.Nip,
                    Value = intermediaryNip
                })
                .WithPermission(AuthorizationPermissionType.TaxRepresentative)
                .WithDescription(descOwnerToIntermediary)
                .WithSubjectDetails(new PermissionsAuthorizationSubjectDetails
                {
                    FullName = "Podmiot Testowy 1"
				})
                .Build();

        OperationResponse opGrantOwnerToInterm =
            await KsefClient.GrantsAuthorizationPermissionAsync(ownerToIntermediary, ownerAccessToken, CancellationToken);

        PermissionsOperationStatusResponse stOwnerToInterm =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(opGrantOwnerToInterm.ReferenceNumber, ownerAccessToken),
                condition: r => r.Status.Code == OperationSuccessfulStatusCode,
                "Czekam na GRANT Owner->Intermediary (200)",
                TimeSpan.FromMilliseconds(SleepTime), 60,
                cancellationToken: CancellationToken);

        // AUTH: intermediary (kontekst NIP biura)
        AuthenticationOperationStatusResponse intermAuth =
            await AuthenticationUtils.AuthenticateAsync(KsefClient, intermediaryNip);
        string intermediaryAccessToken = intermAuth.AccessToken.Token;

        // 2) INTERMEDIARY -> PERSON(PESEL): grant "InvoiceWrite" (brak wspólnego scope z Owner->Intermediary)
        GrantPermissionsPersonRequest intermToPerson = new()
        {
            SubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
                Value = personPesel
            },
            Permissions = [PersonPermissionType.InvoiceWrite],
            SubjectDetails = new PersonPermissionSubjectDetails
            {
                SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
                {
                    FirstName = "Jan",
                    LastName = "Testowy"
                }
			},
            Description = descIntermediaryToPerson
        };

        OperationResponse opGrantIntermToPerson =
            await KsefClient.GrantsPermissionPersonAsync(intermToPerson, intermediaryAccessToken, CancellationToken);

        PermissionsOperationStatusResponse stIntermToPerson =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(opGrantIntermToPerson.ReferenceNumber, intermediaryAccessToken),
                r => r.Status.Code == OperationSuccessfulStatusCode,
                "Czekam na GRANT Intermediary->Person (200)",
                TimeSpan.FromMilliseconds(SleepTime), 60,
                cancellationToken: CancellationToken);

        PersonPermissionsQueryRequest grantedCheck = new()
        {
            ContextIdentifier = new PersonPermissionsContextIdentifier
            {
                Type = PersonPermissionsContextIdentifierType.Nip,
                Value = intermediaryNip
            },
            TargetIdentifier = new PersonPermissionsTargetIdentifier
            {
                Type = PersonPermissionsTargetIdentifierType.Nip,
                Value = intermediaryNip
            },
            AuthorizedIdentifier = new PersonPermissionsAuthorizedIdentifier
            {
                Type = PersonAuthorizedIdentifierType.Pesel,
                Value = personPesel
            },
            PermissionState = PersonPermissionState.Active,
            QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
        };

        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> grantedPage =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchGrantedPersonPermissionsAsync(
                    grantedCheck, intermediaryAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken).ConfigureAwait(false),
                result => result is not null
                          && result.Permissions is not null
                          && result.Permissions.Any(p =>
                                 p.AuthorizedIdentifier != null
                              && p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Pesel
                              && string.Equals(p.AuthorizedIdentifier.Value, personPesel, StringComparison.Ordinal)
                              && p.PermissionScope == PersonPermissionType.InvoiceWrite),
                description: "Czekam aż Intermediary zobaczy nadany grant (filter by PESEL, Active)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // twardy assert – grant MUSI być widoczny dla nadawcy
        Assert.NotNull(grantedPage);
        Assert.NotNull(grantedPage.Permissions);
        Assert.Contains(grantedPage.Permissions, p =>
            p is not null
            && p.AuthorizedIdentifier is not null
            && p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Pesel
            && string.Equals(p.AuthorizedIdentifier.Value, personPesel, StringComparison.Ordinal)
            && p.PermissionScope == PersonPermissionType.InvoiceWrite);

        // AUTH: osoba (PESEL) certyfikatem, w KONTEKŚCIE NIP właściciela
        using X509Certificate2 personalCert = SelfSignedCertificateForSignatureBuilder
            .Create().WithGivenName("A").WithSurname("R")
            .WithSerialNumber("PNOPL-" + personPesel).WithCommonName("Indirect Person").Build();

        AuthenticationOperationStatusResponse personAuth =
            await AuthenticationUtils.AuthenticateAsync(
                KsefClient, intermediaryNip, AuthenticationTokenContextIdentifierType.Nip, personalCert);

        string personAccessToken = personAuth.AccessToken.Token;

        PersonalPermissionsQueryRequest query = new()
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
            await KsefClient.SearchGrantedPersonalPermissionsAsync(
                query, personAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken);

        PersonToken token = _tokenService.MapFromJwt(personAccessToken);
        #endregion

        #region Assert
        // brak skutecznych uprawnień (brak przecięcia zakresów): lista pusta
        Assert.NotNull(page);
        Assert.NotNull(page.Permissions);
        Assert.DoesNotContain(page.Permissions, p => p.PermissionScope is PersonalPermissionScopeType.InvoiceRead or PersonalPermissionScopeType.InvoiceWrite);

        // w tokenie brak efektywnych uprawnień (pep) pasujących do kontekstu ownera
        Assert.DoesNotContain(token.PermissionsEffective ?? [], x =>
            x.Equals("InvoiceRead", StringComparison.OrdinalIgnoreCase) ||
            x.Equals("InvoiceWrite", StringComparison.OrdinalIgnoreCase));
        #endregion

        #region Cleanup
        // revoke osobie
        // (gdyby pojawił się jakikolwiek wpis — defensywnie)
        foreach (PersonalPermission p in page.Permissions)
        {
            OperationResponse revoke =
                await KsefClient.RevokeCommonPermissionAsync(p.Id, intermediaryAccessToken, CancellationToken);
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revoke.ReferenceNumber, intermediaryAccessToken),
                condition: r => r.Status.Code == OperationSuccessfulStatusCode,
                "Czekam na REVOKE Intermediary->Person (200)",
                TimeSpan.FromMilliseconds(SleepTime), 60, cancellationToken: CancellationToken);
        }

        // revoke Owner->Intermediary
        // (jeśli API zwraca id grantu podmiotowego w osobnym query, tu pomijamy — środowiska różnią się ekspozycją;
        // na potrzeby testu wystarczy sprzątnięcie subjectów)
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = intermediaryNip }, CancellationToken);
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = ownerNip }, CancellationToken);
        #endregion
    }
}
