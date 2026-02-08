using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.TestData
{
    public class TestDataE2ETests : TestBase
    {
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
        [InlineData(SubjectType.VatGroup, "Grupa VAT", EntityRoleType.VatGroupUnit)]
        [InlineData(SubjectType.JST, "JST", EntityRoleType.LocalGovernmentUnit)]
        public async Task CreateSubjectWithSubunit_VerifyRoles_ThenRemoveAndVerifyRolesRemoved(
            SubjectType subjectType,
            string subjectDescription,
            EntityRoleType expectedRoleType)
        {
            // Arrange
            string subjectNip = MiscellaneousUtils.GetRandomNip();
            string subunitNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania utworzenia podmiotu głównego wraz z jednostką podrzędną
            SubjectCreateRequest createRequest = new()
            {
                SubjectNip = subjectNip,
                SubjectType = subjectType,
                Subunits =
                [
                    new() {
                        SubjectNip = subunitNip,
                        Description = $"Jednostka podrzędna - {subjectDescription}"
                    }
                ],
                Description = $"{subjectDescription} testowy"
            };

            // Utworzenie testowego podmiotu w systemie
            await TestDataClient.CreateSubjectAsync(createRequest);

            // Uwierzytelnienie jako podmiot główny
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
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
			Assert.True(rolesAfterCreation.Roles.All(x => x.StartDate != default));
			Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == expectedRoleType),
                $"Jednostka podrzędna powinna mieć przypisaną rolę {expectedRoleType}");

            // Usunięcie testowego podmiotu głównego (automatycznie usuwa jednostki podrzędne)
            SubjectRemoveRequest removeRequest = new()
            {
                SubjectNip = subjectNip
            };

            await TestDataClient.RemoveSubjectAsync(removeRequest);

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
            Assert.True(subjectRolesAfterRemoval.Roles.All(x => x.ParentEntityIdentifier != null));
            Assert.True(subjectRolesAfterRemoval.Roles.All(x => x.Description != null));
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
        public async Task CreateEnforcementAuthorityVerifyRoleThenRemoveAndVerifyRoleRemoved()
        {
            // Arrange
            string subjectNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania utworzenia organu egzekucyjnego (bez jednostek podrzędnych)
            SubjectCreateRequest createRequest = new()
            {
                SubjectNip = subjectNip,
                SubjectType = SubjectType.EnforcementAuthority,
                Description = "Organ egzekucyjny testowy"
            };

            // Act - Utworzenie testowego podmiotu w systemie
            await TestDataClient.CreateSubjectAsync(createRequest);

            // Uwierzytelnienie jako organ egzekucyjny
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                subjectNip);

            // Act - Pobranie ról przypisanych do organu egzekucyjnego
            PagedRolesResponse<EntityRole> rolesAfterCreation = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null
                    && r.Roles.Any(role => role.Role == EntityRoleType.EnforcementAuthority),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja przypisania roli EnforcementAuthority po utworzeniu
            Assert.NotNull(rolesAfterCreation);
            Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == EntityRoleType.EnforcementAuthority),
                "Organ egzekucyjny powinien mieć przypisaną rolę EnforcementAuthority");

            // Act - Usunięcie testowego podmiotu
            SubjectRemoveRequest removeRequest = new()
            {
                SubjectNip = subjectNip
            };

            await TestDataClient.RemoveSubjectAsync(removeRequest);

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
            Assert.False(rolesAfterRemoval.Roles.Any(role => role.Role == EntityRoleType.EnforcementAuthority),
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
        public async Task CreatePersonWithBailiffFlagVerifyCourtBailiffRoleThenRemoveAndVerifyRoleRemoved()
        {
            // Arrange
            string personNip = MiscellaneousUtils.GetRandomNip();
            string personPesel = MiscellaneousUtils.GetRandomPesel();

            // Przygotowanie żądania utworzenia osoby fizycznej z flagą komornika
            PersonCreateRequest createRequest = new()
            {
                Nip = personNip,
                Pesel = personPesel,
                IsBailiff = true,
                Description = "Osoba fizyczna testowa z flagą komornika"
            };

            // Utworzenie testowej osoby fizycznej w systemie
            await TestDataClient.CreatePersonAsync(createRequest);

            // Uwierzytelnienie jako utworzona osoba fizyczna
            AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                personNip);

            // Act - Pobranie ról przypisanych do osoby fizycznej
            PagedRolesResponse<EntityRole> rolesAfterCreation = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchEntityInvoiceRolesAsync(authOperationStatusResponse.AccessToken.Token),
                condition: r => r is not null
                    && r.Roles is not null
                    && r.Roles.Any(role => role.Role == EntityRoleType.CourtBailiff),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            // Assert - Weryfikacja przypisania roli CourtBailiff po utworzeniu
            Assert.NotNull(rolesAfterCreation);
            Assert.NotNull(rolesAfterCreation.Roles);
            Assert.True(rolesAfterCreation.Roles.Any(role => role.Role == EntityRoleType.CourtBailiff),
                "Osoba fizyczna z flagą IsBailiff powinna mieć przypisaną rolę CourtBailiff");

            // Act - Usunięcie testowej osoby fizycznej z systemu (usuwa powiązane role)
            PersonRemoveRequest removeRequest = new()
            {
                Nip = personNip
            };

            await TestDataClient.RemovePersonAsync(removeRequest);

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
            Assert.False(rolesAfterRemoval.Roles.Any(role => role.Role == EntityRoleType.CourtBailiff),
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
        public async Task GrantTestDataPermissionsVerifyGrantedThenRevokeAndVerifyRemoved()
        {
            // Arrange
            string ownerNip = MiscellaneousUtils.GetRandomNip();
            string authorizedUserNip = MiscellaneousUtils.GetRandomNip();

            // Przygotowanie żądania nadania uprawnień testowych
            TestDataPermissionsGrantRequest grantRequest = new()
            {
                AuthorizedIdentifier = new AuthorizedIdentifier
                {
                    Type = AuthorizedIdentifierType.Nip,
                    Value = authorizedUserNip
                },
                ContextIdentifier = new Client.Core.Models.TestData.ContextIdentifier
                {
                    Value = ownerNip
                },
                Permissions =
                [
                    new() {
                        PermissionType = PermissionType.InvoiceRead,
                        Description = "Uprawnienie InvoiceRead podmiotu testowego"
                    },
                    new() {
                        PermissionType = PermissionType.InvoiceWrite,
                        Description = "Uprawnienie InvoiceWrite podmiotu testowego"
                    }
                ]
            };

            // Nadanie uprawnień testowych
            await TestDataClient.GrantPermissionsAsync(grantRequest);

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
                            ownerNip,
                            AuthenticationTokenContextIdentifierType.Nip,
                            authorizedUserCertificate).ConfigureAwait(false);
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
            PersonalPermissionsQueryRequest permissionsQuery = new();

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
            TestDataPermissionsRevokeRequest revokeRequest = new()
            {
                AuthorizedIdentifier = new AuthorizedIdentifier
                {
                    Type = AuthorizedIdentifierType.Nip,
                    Value = authorizedUserNip
                },
                ContextIdentifier = new Client.Core.Models.TestData.ContextIdentifier
                {
                    Value = ownerNip
                }
            };

            await TestDataClient.RevokePermissionsAsync(revokeRequest);

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
		/// 1. Nadanie podmiotowi uprawnienia do załączników
		/// 2. Uwierzytelnienie i weryfikacja aktywnego uprawnienia
		/// 3. Cofnięcie uprawnienia - ustawienie daty wygaśnięcia (revokeDate)
		/// 4. Weryfikacja ustawienia daty wygaśnięcia uprawnienia
		/// </summary>
		[Fact]
		public async Task GrantAttachmentPermission_VerifyEnabled_ThenRevokeAndVerifyDisabled()
		{
			// Arrange
			string subjectNip = MiscellaneousUtils.GetRandomNip();
			string revokeDate = DateTime.UtcNow.AddDays(1).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

			// Nadanie uprawnienia do wysyłki faktur z załącznikami
			AttachmentPermissionGrantRequest grantRequest = new AttachmentPermissionGrantRequest
			{
				Nip = subjectNip
			};

			await TestDataClient.EnableAttachmentAsync(grantRequest);

			// Uwierzytelnienie podmiotu
			AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
				AuthorizationClient,
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
				Nip = subjectNip,
				ExpectedEndDate = revokeDate
			};

			await TestDataClient.DisableAttachmentAsync(revokeRequest);

			// Pobranie statusu uprawnienia po cofnięciu z pollingiem - weryfikacja ustawienia daty wygaśnięcia
			PermissionsAttachmentAllowedResponse revokedPermissionStatus = await AsyncPollingUtils.PollAsync(
				action: () => KsefClient.GetAttachmentPermissionStatusAsync(authOperationStatusResponse.AccessToken.Token),
				condition: r => r is not null && r.RevokedDate.HasValue,
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: MaxPollingAttempts,
				cancellationToken: CancellationToken);

			// Assert - Weryfikacja ustawienia daty wygaśnięcia uprawnienia
			Assert.NotNull(revokedPermissionStatus);
			Assert.True(revokedPermissionStatus.RevokedDate.HasValue,
				"Data wygaśnięcia uprawnienia (revokeDate) powinna zostać ustawiona po cofnięciu uprawnienia");

#if NETFRAMEWORK
            DateTime expectedDate = DateTime.Parse(revokeDate, CultureInfo.InvariantCulture);
            DateTime actualDate = revokedPermissionStatus.RevokedDate.Value.ToUniversalTime().Date;
            Assert.Equal(expectedDate, actualDate);
#else
            DateOnly expectedDate = DateOnly.Parse(revokeDate, CultureInfo.InvariantCulture);

            DateOnly actualDate = DateOnly.FromDateTime(revokedPermissionStatus.RevokedDate.Value.ToUniversalTime());

            Assert.Equal(expectedDate, actualDate);
#endif
        }
	}
}
