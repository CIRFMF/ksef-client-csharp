using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.TestData
{
    public class TestDataE2ETests : TestBase
    {
        protected ITestDataClient _testDataClient => _scope.ServiceProvider.GetRequiredService<ITestDataClient>();

        private const string CourtBailiffRole = "CourtBailiff";
        private const string VatGroupUnitRole = "VatGroupUnit";
        private const string LocalGovernmentUnitRole = "LocalGovernmentUnit";
        private const string EnforcementAuthorityRole = "EnforcementAuthority";
        private const int MaxPollingAttempts = 30;

        /// <summary>
        /// Weryfikacja poprawności tworzenia i usuwania testowego podmiotu z jednostką podrzędną.
        /// Dotyczy typów: VatGroup i JST (obsługują jednostki podrzędne).
        /// Scenariusz:
        /// 1. Utworzenie podmiotu z jednostką podrzędną
        /// 2. Uwierzytelnienie i weryfikacja przypisania odpowiednich ról
        /// 3. Usunięcie podmiotu
        /// 4. Weryfikacja usunięcia ról
        /// </summary>
        [Theory]
        [InlineData(SubjectType.VatGroup, "Grupa VAT", VatGroupUnitRole)]
        [InlineData(SubjectType.JST, "JST", LocalGovernmentUnitRole)]
        public async Task CreateSubjectWithSubunit_VerifyRoles_ThenRemoveAndVerifyRolesRemoved(
            SubjectType subjectType,
            string subjectDescription,
            string expectedRoleType)
        {
            // Arrange
            string subjectNip = MiscellaneousUtils.GetRandomNip();
            string subunitNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania utworzenia podmiotu głównego wraz z jednostką podrzędną
            SubjectCreateRequest createRequest = new SubjectCreateRequest
            {
                SubjectNip = subjectNip,
                SubjectType = subjectType,
                Subunits = new List<SubjectSubunit>
                {
                    new SubjectSubunit
                    {
                        SubjectNip = subunitNip,
                        Description = $"Jednostka podrzędna - {subjectDescription}"
                    }
                },
                Description = $"{subjectDescription} testowy"
            };

            // Utworzenie testowego podmiotu w systemie
            await _testDataClient.CreateSubjectAsync(createRequest);

            // Uwierzytelnienie jako podmiot główny
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                subjectNip);

            // Pobranie ról przypisanych do podmiotu głównego
            PagedRolesResponse<EntityRole> rolesAfterCreation = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null
                    && r.Roles.Any(role => role.Role == expectedRoleType),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja przypisania ról po utworzeniu podmiotu głównego
            Assert.NotNull(rolesAfterCreation);
            Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == expectedRoleType),
                $"Jednostka podrzędna powinna mieć przypisaną rolę {expectedRoleType}");

            // Usunięcie testowego podmiotu głównego (automatycznie usuwa jednostki podrzędne)
            SubjectRemoveRequest removeRequest = new SubjectRemoveRequest
            {
                SubjectNip = subjectNip
            };

            await _testDataClient.RemoveSubjectAsync(removeRequest);

            // Pobranie ról podmiotu głównego po usunięciu
            PagedRolesResponse<EntityRole> subjectRolesAfterRemoval = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja usunięcia ról podmiotu głównego
            Assert.NotNull(subjectRolesAfterRemoval);
            Assert.NotNull(subjectRolesAfterRemoval.Roles);
            Assert.False(subjectRolesAfterRemoval.Roles.Any(role => role.Role == expectedRoleType),
                $"Rola {expectedRoleType} powinna zostać usunięta wraz z podmiotem głównym");
        }

        /// <summary>
        /// Weryfikacja poprawności tworzenia i usuwania organu egzekucyjnego.
        /// EnforcementAuthority nie obsługuje jednostek podrzędnych.
        /// Scenariusz:
        /// 1. Utworzenie organu egzekucyjnego (bez jednostek podrzędnych)
        /// 2. Uwierzytelnienie i weryfikacja przypisania roli EnforcementAuthority
        /// 3. Usunięcie organu egzekucyjnego
        /// 4. Weryfikacja usunięcia roli EnforcementAuthority
        /// </summary>
        [Fact]
        public async Task CreateEnforcementAuthority_VerifyRole_ThenRemoveAndVerifyRoleRemoved()
        {
            // Arrange
            string subjectNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania utworzenia organu egzekucyjnego (bez jednostek podrzędnych)
            SubjectCreateRequest createRequest = new SubjectCreateRequest
            {
                SubjectNip = subjectNip,
                SubjectType = SubjectType.EnforcementAuthority,
                Description = "Organ egzekucyjny testowy"
            };

            // Act - Utworzenie testowego podmiotu w systemie
            await _testDataClient.CreateSubjectAsync(createRequest);

            // Uwierzytelnienie jako organ egzekucyjny
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                subjectNip);

            // Act - Pobranie ról przypisanych do organu egzekucyjnego
            PagedRolesResponse<EntityRole> rolesAfterCreation = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null
                    && r.Roles.Any(role => role.Role == EnforcementAuthorityRole),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja przypisania roli EnforcementAuthority po utworzeniu
            Assert.NotNull(rolesAfterCreation);
            Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == EnforcementAuthorityRole),
                "Organ egzekucyjny powinien mieć przypisaną rolę EnforcementAuthority");

            // Act - Usunięcie testowego podmiotu
            SubjectRemoveRequest removeRequest = new SubjectRemoveRequest
            {
                SubjectNip = subjectNip
            };

            await _testDataClient.RemoveSubjectAsync(removeRequest);

            // Act - Pobranie ról po usunięciu organu egzekucyjnego
            PagedRolesResponse<EntityRole> rolesAfterRemoval = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja usunięcia roli EnforcementAuthority
            Assert.NotNull(rolesAfterRemoval);
            Assert.NotNull(rolesAfterRemoval.Roles);
            Assert.False(rolesAfterRemoval.Roles.Any(role => role.Role == EnforcementAuthorityRole),
                "Rola EnforcementAuthority powinna zostać usunięta wraz z organem egzekucyjnym");
        }

        /// <summary>
        /// Weryfikacja poprawności tworzenia i usuwania osoby fizycznej z flagą komornika.
        /// Scenariusz:
        /// 1. Utworzenie osoby fizycznej z flagą komornika (IsBailiff = true)
        /// 2. Uwierzytelnienie i weryfikacja przypisania roli CourtBailiff
        /// 3. Usunięcie osoby fizycznej
        /// 4. Weryfikacja usunięcia roli CourtBailiff
        /// </summary>
        [Fact]
        public async Task CreatePersonWithBailiffFlag_VerifyCourtBailiffRole_ThenRemoveAndVerifyRoleRemoved()
        {
            // Arrange
            string personNip = MiscellaneousUtils.GetRandomNip();
            string personPesel = MiscellaneousUtils.GetRandomPesel();

            // Przygotowanie żądania utworzenia osoby fizycznej z flagą komornika
            PersonCreateRequest createRequest = new PersonCreateRequest
            {
                Nip = personNip,
                Pesel = personPesel,
                IsBailiff = true,
                Description = "Osoba fizyczna testowa z flagą komornika"
            };

            // Utworzenie testowej osoby fizycznej w systemie
            await _testDataClient.CreatePersonAsync(createRequest);

            // Uwierzytelnienie jako utworzona osoba fizyczna
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                personNip);

            // Act - Pobranie ról przypisanych do osoby fizycznej
            PagedRolesResponse<EntityRole> rolesAfterCreation = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null
                    && r.Roles.Any(role => role.Role == CourtBailiffRole),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja przypisania roli CourtBailiff po utworzeniu
            Assert.NotNull(rolesAfterCreation);
            Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == CourtBailiffRole),
                "Osoba fizyczna z flagą IsBailiff powinna mieć przypisaną rolę CourtBailiff");

            // Act - Usunięcie testowej osoby fizycznej z systemu (usuwa powiązane role)
            PersonRemoveRequest removeRequest = new PersonRemoveRequest
            {
                Nip = personNip
            };

            await _testDataClient.RemovePersonAsync(removeRequest);

            // Pobranie ról po usunięciu osoby
            PagedRolesResponse<EntityRole> rolesAfterRemoval = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja usunięcia roli CourtBailiff wraz z osobą fizyczną
            Assert.NotNull(rolesAfterRemoval);
            Assert.NotNull(rolesAfterRemoval.Roles);
            Assert.False(rolesAfterRemoval.Roles.Any(role => role.Role == CourtBailiffRole),
                "Rola CourtBailiff powinna zostać usunięta wraz z osobą fizyczną");
        }

        /// <summary>
        /// Weryfikuje pełny cykl życia uprawnień testowych: nadanie, sprawdzenie i cofnięcie.
        /// Scenariusz:
        /// 1. Nadanie uprawnień InvoiceRead i InvoiceWrite użytkownikowi testowemu w kontekście NIP
        /// 2. Uwierzytelnienie jako uprawniony użytkownik i weryfikacja nadanych uprawnień
        /// 3. Cofnięcie uprawnień
        /// 4. Weryfikacja, że uprawnienia zostały usunięte
        /// </summary>
        [Fact]
        public async Task GrantTestDataPermissions_VerifyGranted_ThenRevokeAndVerifyRemoved()
        {
            // Arrange
            string ownerNip = MiscellaneousUtils.GetRandomNip();
            string authorizedUserNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania nadania uprawnień testowych
            TestDataPermissionsGrantRequest grantRequest = new TestDataPermissionsGrantRequest
            {
                AuthorizedIdentifier = new AuthorizedIdentifier
                {
                    Type = AuthorizedIdentifierType.Nip,
                    Value = authorizedUserNip
                },
                ContextIdentifier = new KSeF.Client.Core.Models.TestData.ContextIdentifier
                {
                    Value = ownerNip
                },
                Permissions = new List<Permission>
                {
                    new Permission
                    {
                        PermissionType = PermissionType.InvoiceRead,
                        Description = "Uprawnienie InvoiceRead dla podmiotu testowego"
                    },
                    new Permission
                    {
                        PermissionType = PermissionType.InvoiceWrite,
                        Description = "Uprawnienie InvoiceWrite dla podmiotu testowego"
                    }
                }
            };

            // Nadanie uprawnień testowych
            await _testDataClient.GrantPermissionsAsync(grantRequest);

            // Przygotowanie certyfikatu uprawnionego podmiotu
            X509Certificate2 authorizedUserCertificate = CertificateUtils.GetPersonalCertificate(
                givenName: "Paweł",
                surname: "Testowy",
                serialNumberPrefix: "TINPL",
                serialNumber: authorizedUserNip,
                commonName: "Paweł Testowy");

            // Uwierzytelnienie jako uprawniony podmiot w kontekście właściciela z pollingiem
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AsyncPollingUtils.PollAsync(
                action: async () =>
                {
                    try
                    {
                        return await AuthenticationUtils.AuthenticateAsync(
                            AuthorizationClient,
                            SignatureService,
                            ownerNip,
                            AuthenticationTokenContextIdentifierType.Nip,
                            authorizedUserCertificate);
                    }
                    catch
                    {
                        return null;
                    }
                },
                condition: authOperationStatusResponse => authOperationStatusResponse is not null,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Act - Pobranie wszystkich uprawnień uprawnionego podmiotu
            PersonalPermissionsQueryRequest permissionsQuery = new PersonalPermissionsQueryRequest();

            PagedPermissionsResponse<PersonalPermission> grantedPermissions = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    permissionsQuery,
                    authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                        && r.Permissions is not null
                        && r.Permissions.Count > 0,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja nadanych uprawnień
            Assert.NotNull(grantedPermissions);
            Assert.NotNull(grantedPermissions.Permissions);
            Assert.True(grantedPermissions.Permissions.Any(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead),
                "Nie nadano uprawnienia InvoiceRead");
            Assert.True(grantedPermissions.Permissions.Any(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceWrite),
                "Nie nadano uprawnienia InvoiceWrite");

            // Act - Cofnięcie uprawnień
            TestDataPermissionsRevokeRequest revokeRequest = new TestDataPermissionsRevokeRequest
            {
                AuthorizedIdentifier = new AuthorizedIdentifier
                {
                    Type = AuthorizedIdentifierType.Nip,
                    Value = authorizedUserNip
                },
                ContextIdentifier = new KSeF.Client.Core.Models.TestData.ContextIdentifier
                {
                    Value = ownerNip
                }
            };

            await _testDataClient.RevokePermissionsAsync(revokeRequest);

            // Pobranie uprawnień ponownie po cofnięciu
            PagedPermissionsResponse<PersonalPermission> revokedPermissions = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    permissionsQuery,
                    authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                        && r.Permissions is not null,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            Assert.NotNull(revokedPermissions);
            Assert.False(revokedPermissions.Permissions.Any(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead),
                "Uprawnienie InvoiceRead powinno zostać usunięte po cofnięciu");
            Assert.False(revokedPermissions.Permissions.Any(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceWrite),
                "Uprawnienie InvoiceWrite powinno zostać usunięte po cofnięciu");
        }

        /// <summary>
        /// Weryfikacja poprawności nadawania i odbierania uprawnień do wysyłki faktur z załącznikami.
        /// Scenariusz:
        /// 1. Nadanie uprawnienia do załączników dla podmiotu
        /// 2. Uwierzytelnienie i weryfikacja aktywnego uprawnienia
        /// 3. Cofnięcie uprawnienia
        /// 4. Weryfikacja usunięcia uprawnienia
        /// </summary>
        // [Fact]
        public async Task GrantAttachmentPermission_VerifyEnabled_ThenRevokeAndVerifyDisabled()
        {
            // Arrange
            string subjectNip = MiscellaneousUtils.GetRandomNip();

            // Nadanie uprawnienia do wysyłki faktur z załącznikami
            AttachmentPermissionGrantRequest grantRequest = new AttachmentPermissionGrantRequest
            {
                Nip = subjectNip
            };

            await _testDataClient.EnableAttachmentAsync(grantRequest);

            // Uwierzytelnienie podmiotu
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                subjectNip);

            // Act - Pobranie statusu uprawnienia do załączników z pollingiem
            PermissionsAttachmentAllowedResponse grantedPermissionStatus = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.GetAttachmentPermissionStatusAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null && r.IsAttachmentAllowed == true,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja nadania uprawnienia
            Assert.NotNull(grantedPermissionStatus);
            Assert.True(grantedPermissionStatus.IsAttachmentAllowed,
                "Uprawnienie do wysyłki załączników powinno być aktywne po nadaniu");

            // Act - Cofnięcie uprawnienia do wysyłki faktur z załącznikami
            AttachmentPermissionRevokeRequest revokeRequest = new AttachmentPermissionRevokeRequest
            {
                Nip = subjectNip
            };

            await _testDataClient.DisableAttachmentAsync(revokeRequest);

            // Pobranie statusu uprawnienia po cofnięciu z pollingiem
            PermissionsAttachmentAllowedResponse revokedPermissionStatus = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.GetAttachmentPermissionStatusAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null && r.IsAttachmentAllowed == false,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja cofnięcia uprawnienia
            Assert.NotNull(revokedPermissionStatus);
            Assert.False(revokedPermissionStatus.IsAttachmentAllowed,
                "Uprawnienie do wysyłki załączników powinno być nieaktywne po cofnięciu");
        }
    }
}
