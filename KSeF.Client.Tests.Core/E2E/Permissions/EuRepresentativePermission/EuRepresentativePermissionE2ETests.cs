#nullable enable
using KSeF.Client.Api.Builders.EuEntityPermissions;
using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Tests.Utils;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuRepresentativePermission;

public class EuRepresentativePermissionE2ETests : TestBase
{
    /*
     * 1. Uwierzytelnienie jako właściciel - kontekst NIP
     * 2. Właściciel nadaje uprawnienia administracyjne jednostce organizacyjnej - kontekst NipVatEu 
     * 3. Pobranie uprawnień nadanych kontekstowi
     * 4. Uwierzytelnienie jako jednostka organizacyjna - kontekst NipVatEu
     * 5. W tokenie powinny być role jednostki
     * 6. Nadanie uprawnień reprezentanta
     * 7. Pobranie/sprawdzenie nadanych uprawnień
     * 8. Odwołanie uprawnień (wymaga odczekania kilku sekund...)
     * 9. Sprawdzenie uprawnień po odwołaniu
     */

    [Fact]
    public async Task GrantAdministrativePermission_E2E_ReturnsExpectedResults()
    {
        // Przygotowanie
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string ownerVatEu = MiscellaneousUtils.GetRandomNipVatEU(ownerNip);

        string euEntityNip = MiscellaneousUtils.GetRandomNip();
        string euEntityVatEu = MiscellaneousUtils.GetRandomNipVatEU(euEntityNip);

        string euRepresentativeEntityNip = MiscellaneousUtils.GetRandomNip();
        string euRepresentativeEntityVatEu = MiscellaneousUtils.GetRandomNipVatEU(euRepresentativeEntityNip);
      
        X509Certificate2 ownerCertificate = CertificateUtils.GetPersonalCertificate("Jan", "Kowalski", "TINPL", ownerNip, "M B");
        string ownerCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(ownerCertificate);

        X509Certificate2 euEntitySealCertificate = CertificateUtils.GetCompanySeal("Kowalski sp. z o.o", euEntityVatEu, "Kowalski");
        string euEntitySealCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euEntitySealCertificate);

        X509Certificate2 euEntityPersonalCertificate = CertificateUtils.GetPersonalCertificate("Paweł", "Testowy", "TINPL", euEntityNip, "T P");
        string euEntityPersonalCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euEntityPersonalCertificate);

        X509Certificate2 euRepresentativeEntityCertificate = CertificateUtils.GetPersonalCertificate(
            "Reprezentant M",
            "Reprezentant B",
            "TINPL",
            euRepresentativeEntityNip,
            "M B"
            );
        string euRepresentativeEntityCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euRepresentativeEntityCertificate);

        // Działanie
        // 1) Uwierzytelnij właściciela (kontekst NIP)
        AuthenticationOperationStatusResponse ownerAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            ownerNip,
            AuthenticationTokenContextIdentifierType.Nip,
            ownerCertificate);

        //2) Właściciel nadaje uprawnienia administracyjne jednostce unijnej(kontekst NipVatEu)
        PermissionsEuEntitySubjectDetails subjectDetails = new PermissionsEuEntitySubjectDetails
        {
            SubjectDetailsType = PermissionsEuEntitySubjectDetailsType.EntityByFingerprint,
            EntityByFp = new PermissionsEuEntityEntityByFp
            {
                Address = "EU Admin Address",
                FullName = "EU Admin Full Name"
            }
        };

        GrantPermissionsEuEntityRequest grantAdministrativePermissionsRequest = GrantEuEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(new EuEntitySubjectIdentifier
            {
                Type = EuEntitySubjectIdentifierType.Fingerprint,
                Value = euEntityPersonalCertificateFingerprint
            })
            .WithSubjectName("MB Company")
            .WithContext(new EuEntityContextIdentifier { Type = EuEntityContextIdentifierType.NipVatUe, Value = ownerVatEu })
            .WithDescription("EU Company")
            .WithSubjectDetails(subjectDetails)
            .WithEuEntityDetails(new PermissionsEuEntityDetails
            {
                Address = "ul. Testowa 1, 00-000 Miasto",
                FullName = "Podmiot Testowy 1"
            })
            .Build();

        OperationResponse response = await KsefClient
            .GrantsPermissionEUEntityAsync(grantAdministrativePermissionsRequest, ownerAuthInfo.AccessToken.Token, CancellationToken);

        // 3) Odpytywanie, aż uprawnienia nadane kontekstowi będą widoczne
        EuEntityPermissionsQueryRequest permissionsQueryRequest = new() { };

        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> grantedPermissions = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, ownerAuthInfo.AccessToken.Token).ConfigureAwait(false),
            condition: result => result is not null && result.Permissions is { Count: > 0 },
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // 4) Uwierzytelnij jako jednostka unijna (kontekst NipVatEu) używając certyfikatu osobistego jednostki unijnej
        AuthenticationOperationStatusResponse euAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            ownerVatEu, // nipvateu kontekstu
            AuthenticationTokenContextIdentifierType.NipVatUe, // typ identyfikatora kontekstu
            euEntityPersonalCertificate, // certyfikat jednostki eu
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateFingerprint); // typ identyfikatora jednostki eu

        // 6) Nadaj uprawnienia reprezentanta
        EuEntityRepresentativeSubjectDetails euRepresentativeSubjectDetails = new EuEntityRepresentativeSubjectDetails
        {
            SubjectDetailsType = EuEntityRepresentativeSubjectDetailsType.PersonByFingerprintWithoutIdentifier,
            PersonByFpNoId = new EuEntityRepresentativePersonByFpNoId
            {
                FirstName = "Reprezentant",
                LastName = "Reprezentant",
                BirthDate = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IdDocument = new EuEntityRepresentativeIdentityDocument
                {
                    Type = "Passport",
                    Number = "AA1234567",
                    Country = "PL"
                }
            }
        };

        GrantPermissionsEuEntityRepresentativeRequest grantRepresentativePermissionsRequest =
            GrantEUEntityRepresentativePermissionsRequestBuilder
                .Create()
                .WithSubject(new EuEntityRepresentativeSubjectIdentifier
                {
                    Type = EuEntityRepresentativeSubjectIdentifierType.Fingerprint,
                    Value = euRepresentativeEntityCertificateFingerprint
                })
                .WithPermissions(
                    EuEntityRepresentativeStandardPermissionType.InvoiceWrite,
                    EuEntityRepresentativeStandardPermissionType.InvoiceRead
                )
                .WithDescription("Representative for EU Entity")
                .WithSubjectDetails(euRepresentativeSubjectDetails)
                .Build();

        OperationResponse grantRepresentativePermissionResponse =
            await KsefClient.GrantsPermissionEUEntityRepresentativeAsync(
                grantRepresentativePermissionsRequest,
                euAuthInfo.AccessToken.Token,
                CancellationToken);

        // 7) Odpytywanie aż uprawnienia reprezentanta będą widoczne
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> grantedRepresentativePermission = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, euAuthInfo.AccessToken.Token).ConfigureAwait(false),
            condition: result => result is not null 
                       && result.Permissions is { Count: > 0} 
                       && result.Permissions.Any(p => p.AuthorizedFingerprintIdentifier == euRepresentativeEntityCertificateFingerprint),
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // 8) Odwołaj uprawnienia reprezentanta (wszystkie zwrócone)
        List<PermissionsOperationStatusResponse> unsuccessfulRemovedPermissions = [];
        List<OperationResponse> referenceList = [];
        foreach (Client.Core.Models.Permissions.EuEntityPermission? permission in grantedRepresentativePermission.Permissions)
        {
            OperationResponse revokeCommonPermissionAsyncReference = await KsefClient.RevokeCommonPermissionAsync(permission.Id, euAuthInfo.AccessToken.Token);
            referenceList.Add(revokeCommonPermissionAsyncReference);            
        }

        Thread.Sleep(SleepTime);

        foreach (OperationResponse revokeCommonPermissionAsyncReference in referenceList)
        {
            PermissionsOperationStatusResponse revokeCommonPermissionAsyncStatus = await KsefClient.OperationsStatusAsync(revokeCommonPermissionAsyncReference.ReferenceNumber, euAuthInfo.AccessToken.Token);

            if (revokeCommonPermissionAsyncStatus.Status.Code != OperationStatusCodeResponse.Success)
            {
                unsuccessfulRemovedPermissions.Add(revokeCommonPermissionAsyncStatus);
            }
        }


        Assert.Empty(unsuccessfulRemovedPermissions);

        // 9) Odpytywanie aż uprawnienia znikną
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> afterRevoke = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, euAuthInfo.AccessToken.Token).ConfigureAwait(false),
            condition: result => result is not null && (result.Permissions is null || result.Permissions.Count == 0),
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // Weryfikacja
        Assert.NotNull(grantedPermissions);
        Assert.NotEmpty(grantedPermissions.Permissions);
        Assert.Contains(grantedPermissions.Permissions, x => x.SubjectEntityDetails.Address == subjectDetails.EntityByFp.Address 
        && x.SubjectEntityDetails.FullName == subjectDetails.EntityByFp.FullName);

        Assert.NotNull(euAuthInfo);
        Assert.NotNull(euAuthInfo.AccessToken);
        Assert.NotNull(euAuthInfo.RefreshToken);

        Client.Core.Models.Token.PersonToken personTokenInfo = TokenService.MapFromJwt(euAuthInfo.AccessToken.Token);
        Assert.NotNull(personTokenInfo);

        Assert.NotNull(grantRepresentativePermissionResponse);
        Assert.NotNull(grantedRepresentativePermission);
        Assert.NotEmpty(grantedRepresentativePermission.Permissions);
        Assert.True(grantedRepresentativePermission.Permissions.Where(x => x.SubjectPersonDetails != null).Count(x => x.SubjectPersonDetails.FirstName == euRepresentativeSubjectDetails.PersonByFpNoId.FirstName && x.SubjectPersonDetails.LastName == euRepresentativeSubjectDetails.PersonByFpNoId.LastName) == 2);


        Assert.NotNull(afterRevoke);
        Assert.Empty(afterRevoke.Permissions);
    }
}