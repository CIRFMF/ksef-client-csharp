using KSeF.Client.Api.Builders.EuEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuEntityPermission
{
    /// <summary>
    /// Testy E2E nadawania uprawnień administracyjnych jednostkom UE i uwierzytelniania w ich kontekście.
    /// </summary>
    public class EuEntityPermissionAdminAuthenticationE2ETests : TestBase
    {
        private const string AdminSubjectName = "EU Admin Entity";
        private const string AdminPermissionDescription = "E2E EU Entity Admin Permission Test";

        /// <summary>
        /// Test kompletnego scenariusza nadania uprawnień administracyjnych:
        /// 1. Owner loguje się w kontekście NIP
        /// 2. Owner nadaje uprawnienia administracyjne jednostce UE w kontekście NipVatUe
        /// 3. Weryfikacja statusu operacji nadania uprawnień
        /// 4. Wyszukanie nadanych uprawnień w systemie
        /// 5. Uwierzytelnienie jako administrator i weryfikacja uprawnień w tokenie JWT
        /// </summary>
        [Fact]
        public async Task GrantEuEntityAdminPermission_AuthenticateAndVerifyPermissions_Success()
        {
            #region Przygotowanie danych testowych

            // Generowanie danych właściciela (główny podmiot nadający uprawnienia)
            string ownerNip = MiscellaneousUtils.GetRandomNip();
            string ownerVatEu = MiscellaneousUtils.GetRandomNipVatEU(ownerNip);

            // Generowanie danych jednostki UE (przyszły administrator)
            string euAdminNip = MiscellaneousUtils.GetRandomNip();
            string euAdminVatEu = MiscellaneousUtils.GetRandomNipVatEU(euAdminNip);


            // Tworzenie certyfikatów właścicielowi i administratorowi
            X509Certificate2 ownerCertificate = CertificateUtils.GetPersonalCertificate(
                givenName: "Jan",
                surname: "Kowalski",
                serialNumberPrefix: "TINPL",
                serialNumber: ownerNip,
                commonName: "J K");

            X509Certificate2 euAdminCertificate = CertificateUtils.GetPersonalCertificate(
                givenName: "Admin",
                surname: "Administrator",
                serialNumberPrefix: "TINPL",
                serialNumber: euAdminNip,
                commonName: "A A");

            string euAdminCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euAdminCertificate);

            #endregion

            #region Uwierzytelnienie jako owner (kontekst NIP)

            // Uwierzytelnienie właściciela w kontekście NIP
            AuthenticationOperationStatusResponse ownerAuthResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                ownerNip,
                AuthenticationTokenContextIdentifierType.Nip,
                ownerCertificate);

            Assert.NotNull(ownerAuthResponse);
            Assert.NotNull(ownerAuthResponse.AccessToken);
            Assert.False(string.IsNullOrEmpty(ownerAuthResponse.AccessToken.Token),
                "Wartość access tokena ownera nie powinna być pusta");

            string ownerAccessToken = ownerAuthResponse.AccessToken.Token;

            #endregion

            #region Nadanie uprawnień administracyjnych jednostce EU
            PermissionsEuEntitySubjectDetails subjectDetails = new PermissionsEuEntitySubjectDetails
            {
                SubjectDetailsType = PermissionsEuEntitySubjectDetailsType.EntityByFingerprint,
                EntityByFp = new PermissionsEuEntityEntityByFp
                {
                    Address = "EU Admin Address",
                    FullName = "EU Admin Full Name"
                }
            };

            // Właściciel nadaje uprawnienia administracyjne jednostce UE
            GrantPermissionsEuEntityRequest grantPermissionsRequest = GrantEuEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(new EuEntitySubjectIdentifier
                {
                    Type = EuEntitySubjectIdentifierType.Fingerprint,
                    Value = euAdminCertificateFingerprint
                })
                .WithSubjectName(AdminSubjectName)
                .WithContext(new EuEntityContextIdentifier
                {
                    Type = EuEntityContextIdentifierType.NipVatUe,
                    Value = ownerVatEu
                })
                .WithDescription(AdminPermissionDescription)
				.WithEuEntityDetails(new PermissionsEuEntityDetails
				{
					Address = "ul. Testowa 2, 00-000 Miasto",
					FullName = "Podmiot Testowy 2"
				})
				.WithSubjectDetails(subjectDetails)
                .Build();

            OperationResponse grantOperationResponse = await KsefClient.GrantsPermissionEUEntityAsync(
                grantPermissionsRequest,
                ownerAccessToken,
                CancellationToken.None);

            Assert.NotNull(grantOperationResponse);
            Assert.False(string.IsNullOrEmpty(grantOperationResponse.ReferenceNumber),
                "Numer referencyjny operacji nadania uprawnień nie powinien być pusty");

            #endregion

            #region Sprawdzenie statusu operacji nadania uprawnień

            // Odpytywanie statusu operacji nadania uprawnień do momentu sukcesu
            PermissionsOperationStatusResponse grantOperationStatus = await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperationResponse.ReferenceNumber, ownerAccessToken).ConfigureAwait(false),
                status => status is not null &&
                         status.Status is not null &&
                         status.Status.Code == OperationStatusCodeResponse.Success,
                delay: TimeSpan.FromSeconds(1),
                maxAttempts: 60,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(grantOperationStatus);
            Assert.NotNull(grantOperationStatus.Status);
            Assert.True(
                OperationStatusCodeResponse.Success == grantOperationStatus.Status.Code,
                $"Operacja nadania uprawnień powinna zakończyć się sukcesem (kod {OperationStatusCodeResponse.Success}), otrzymano {grantOperationStatus.Status.Code}. " +
                $"Opis: {grantOperationStatus.Status.Description}"
            );

            #endregion

            #region Wyszukanie nadanych uprawnień

            // Wyszukanie nadanych uprawnień w systemie
            EuEntityPermissionsQueryRequest queryRequest = new();

            PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> grantedPermissionsResponse =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(
                        queryRequest,
                        ownerAccessToken,
                        pageOffset: 0,
                        pageSize: 10,
                        CancellationToken.None).ConfigureAwait(false),
                    result => result is not null && result.Permissions is { Count: > 0 },
                    delay: TimeSpan.FromSeconds(1),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken.None);

            Assert.NotNull(grantedPermissionsResponse);
            Assert.NotEmpty(grantedPermissionsResponse.Permissions);
            Assert.True(grantedPermissionsResponse.Permissions.All(x=> x.AuthorIdentifier != null));
            Assert.True(grantedPermissionsResponse.Permissions.All(x=> x.Description != null));
            Assert.True(grantedPermissionsResponse.Permissions.All(x=> x.EuEntityDetails != null));
            Assert.True(grantedPermissionsResponse.Permissions.All(x=> x.SubjectPersonDetails == null));
            Assert.True(grantedPermissionsResponse.Permissions.All(x=> x.AuthorizedFingerprintIdentifier != null));
            Assert.Contains(grantedPermissionsResponse.Permissions, x => x.SubjectEntityDetails.Address == subjectDetails.EntityByFp.Address
            && x.SubjectEntityDetails.FullName == subjectDetails.EntityByFp.FullName);

            #endregion

            #region Uwierzytelnienie jako administrator i weryfikacja uprawnień

            // Uwierzytelnienie jako administrator w kontekście NipVatUe właściciela
            AuthenticationOperationStatusResponse euAdminAuthResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                ownerVatEu, 
                AuthenticationTokenContextIdentifierType.NipVatUe,
                euAdminCertificate, 
                AuthenticationTokenSubjectIdentifierTypeEnum.CertificateFingerprint);

            Assert.NotNull(euAdminAuthResponse);
            Assert.NotNull(euAdminAuthResponse.AccessToken);
            Assert.False(string.IsNullOrEmpty(euAdminAuthResponse.AccessToken.Token),
                "Wartość access tokena administratora EU nie powinna być pusta");

            // Parsowanie tokena JWT i weryfikacja uprawnień administratora
            Client.Core.Models.Token.PersonToken adminTokenInfo = TokenService.MapFromJwt(euAdminAuthResponse.AccessToken.Token);

            Assert.NotNull(adminTokenInfo);
            Assert.NotNull(adminTokenInfo.Permissions);

            // Weryfikacja pełnego zestawu uprawnień administratora
            HashSet<string> expectedAdminPermissions = new()
            {
                EuEntityPermissionType.VatUeManage.ToString(),
                EuEntityPermissionType.InvoiceWrite.ToString(),
                EuEntityPermissionType.InvoiceRead.ToString(),
                EuEntityPermissionType.Introspection.ToString(),
                EuEntityPermissionType.CollectiveIdentifierManage.ToString()
            };

            HashSet<string> actualAdminPermissions = [.. adminTokenInfo.Permissions];

            Assert.True(
                expectedAdminPermissions.OrderBy(x => x).SequenceEqual(actualAdminPermissions.OrderBy(x => x)),
                $"Administrator powinien posiadać wszystkie oczekiwane uprawnienia. " +
                $"Oczekiwane: [{string.Join(", ", expectedAdminPermissions)}], " +
                $"Rzeczywiste: [{string.Join(", ", actualAdminPermissions)}]"
            );

            #endregion
        }
    }
}
