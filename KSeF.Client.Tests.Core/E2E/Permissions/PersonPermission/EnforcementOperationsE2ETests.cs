using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

/// <summary>
/// Nadawanie uprawnień do wykonywania operacji komorniczych w kontekście, który na to zezwala
/// (EnforcementAuthority oraz CourtBailiff).
/// </summary>
public class EnforcementOperationsE2ETests : TestBase
{
    private const string PermissionDescription = "E2E grant EnforcementOperations";

    /// <summary>
    /// Scenariusz dla EnforcementAuthority:
    /// 1) Utworzenie podmiotu typu EnforcementAuthority (kontekst dozwolony)
    /// 2) Uwierzytelnienie w kontekście EnforcementAuthority
    /// 3) Nadanie osobie uprawnienia EnforcementOperations
    /// 4) Weryfikacja, że uprawnienie pojawiło się w wyszukiwaniu
    /// 5) Cofnięcie uprawnienia i weryfikacja, że zniknęło
    /// 6) Sprzątanie środowiska testowego (usunięcie podmiotu)
    /// </summary>
    [Fact]
    public async Task GrantEnforcementOperations_AsEnforcementAuthority_E2E_GrantSearchRevokeSearch()
    {
        // Arrange: Utworzenie kontekstu EnforcementAuthority oraz uwierzytelnienie
        string enforcementAuthorityNip = MiscellaneousUtils.GetRandomNip();
        string authorizedNip = MiscellaneousUtils.GetRandomNip();

        await CreateEnforcementAuthorityAsync(enforcementAuthorityNip);

        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, enforcementAuthorityNip);

        string accessToken = authorizationInfo.AccessToken.Token;

        // Act: Nadawanie uprawnienia EnforcementOperations dla osoby uwierzytelniającej się nipem `authorizedNip`
        OperationResponse grantResponse = await GrantEnforcementOperationsAsync(authorizedNip, accessToken);

        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // Wyszukiwanie - trwa do czasu aż pojawi się nadane uprawnienie
        PagedPermissionsResponse<PersonPermission> searchAfterGrant = await AsyncPollingUtils.PollAsync(
            action: async () => await SearchGrantedPersonPermissionsAsync(accessToken),
            condition: result =>
            {
                if (result is null || result.Permissions is null) return false;

                List<PersonPermission> matches = result.Permissions
                    .Where(p => p.Description == PermissionDescription)
                    .ToList();

                return matches.Any(x =>
                    Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.EnforcementOperations);
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        // Assert: Weryfikacja, że uprawnienie jest widoczne i zawiera oczekiwany zakres
        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.Contains(searchAfterGrant.Permissions,
            x => x.Description == PermissionDescription &&
                 Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.EnforcementOperations);

        // Wycofanie wszystkich znalezionych uprawnień (po opisie)
        List<PersonPermission> toRevoke = searchAfterGrant.Permissions
            .Where(p => p.Description == PermissionDescription)
            .ToList();

        List<PermissionsOperationStatusResponse> revokeStatuses = await RevokePermissionsAsync(toRevoke, accessToken);

        Assert.NotNull(revokeStatuses);
        Assert.NotEmpty(revokeStatuses);
        Assert.Equal(toRevoke.Count, revokeStatuses.Count);
        Assert.All(revokeStatuses, s =>
            Assert.Equal(OperationStatusCodeResponse.Success, s.Status.Code));

        // Ponowne wyszukiwanie — aż uprawnienie zniknie
        PagedPermissionsResponse<PersonPermission> searchAfterRevoke = await AsyncPollingUtils.PollAsync(
            action: async () => await SearchGrantedPersonPermissionsAsync(accessToken),
            condition: result =>
            {
                if (result is null || result.Permissions is null) return false;

                List<PersonPermission> remaining = result.Permissions
                    .Where(p => p.Description == PermissionDescription)
                    .ToList();

                return remaining.Count == 0;
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        // Assert: Uprawnienie zostało usunięte
        Assert.NotNull(searchAfterRevoke);
        Assert.True(searchAfterRevoke.Permissions is null ||
                    !searchAfterRevoke.Permissions.Any(p => p.Description == PermissionDescription));

        // Cleanup: Usunięcie kontekstu EnforcementAuthority
        await RemoveEnforcementAuthorityAsync(enforcementAuthorityNip);
    }

    /// <summary>
    /// Scenariusz dla CourtBailiff:
    /// 1) Utworzenie osoby fizycznej z flagą komornika (IsBailiff = true)
    /// 2) Uwierzytelnienie i weryfikacja roli CourtBailiff
    /// 3) Nadanie osobie uprawnienia EnforcementOperations
    /// 4) Weryfikacja, że uprawnienie pojawiło się w wyszukiwaniu
    /// 5) Cofnięcie uprawnienia i weryfikacja, że zniknęło
    /// 6) Sprzątanie środowiska testowego (usunięcie osoby fizycznej)
    /// </summary>
    [Fact]
    public async Task GrantEnforcementOperations_AsCourtBailiff_E2E_GrantSearchRevokeSearch()
    {
        // Arrange: Utworzenie osoby fizycznej z flagą komornika oraz uwierzytelnienie
        string bailiffNip = MiscellaneousUtils.GetRandomNip();
        string bailiffPesel = MiscellaneousUtils.GetRandomPesel();
        string granteeNip = MiscellaneousUtils.GetRandomNip();

        await CreateCourtBailiffAsync(bailiffNip, bailiffPesel);

        AuthenticationOperationStatusResponse auth = await AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, bailiffNip);

        string accessToken = auth.AccessToken.Token;

        // Act: Nadanie uprawnienia EnforcementOperations dla osoby granteeNip
        OperationResponse grantResponse = await GrantEnforcementOperationsAsync(granteeNip, accessToken);

        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // Wyszukiwanie — aż pojawi się nadane uprawnienie
        PagedPermissionsResponse<PersonPermission> searchAfterGrant = await AsyncPollingUtils.PollAsync(
            action: async () => await SearchGrantedPersonPermissionsAsync(accessToken),
            condition: result =>
            {
                if (result is null || result.Permissions is null) return false;

                List<PersonPermission> matches = result.Permissions
                    .Where(p => p.Description == PermissionDescription)
                    .ToList();

                return matches.Any(x =>
                    Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.EnforcementOperations);
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        // Assert: Weryfikacja, że uprawnienie jest widoczne i zawiera oczekiwany zakres
        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.Contains(searchAfterGrant.Permissions,
            x => x.Description == PermissionDescription &&
                 Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.EnforcementOperations);

        // Wycofanie wszystkich znalezionych uprawnień (po opisie)
        List<PersonPermission> toRevoke = searchAfterGrant.Permissions
            .Where(p => p.Description == PermissionDescription)
            .ToList();

        List<PermissionsOperationStatusResponse> revokeStatuses = await RevokePermissionsAsync(toRevoke, accessToken);

        Assert.NotNull(revokeStatuses);
        Assert.NotEmpty(revokeStatuses);
        Assert.Equal(toRevoke.Count, revokeStatuses.Count);
        Assert.All(revokeStatuses, s =>
            Assert.Equal(OperationStatusCodeResponse.Success, s.Status.Code));

        // Ponowne wyszukiwanie — aż uprawnienie zniknie
        PagedPermissionsResponse<PersonPermission> searchAfterRevoke = await AsyncPollingUtils.PollAsync(
            action: async () => await SearchGrantedPersonPermissionsAsync(accessToken),
            condition: result =>
            {
                if (result is null || result.Permissions is null) return false;

                List<PersonPermission> remaining = result.Permissions
                    .Where(p => p.Description == PermissionDescription)
                    .ToList();

                return remaining.Count == 0;
            },
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        // Assert: Uprawnienie zostało usunięte
        Assert.NotNull(searchAfterRevoke);
        Assert.True(searchAfterRevoke.Permissions is null ||
                    !searchAfterRevoke.Permissions.Any(p => p.Description == PermissionDescription));

        // Cleanup: Usunięcie osoby fizycznej z flagą komornika
        await RemoveCourtBailiffAsync(bailiffNip);
    }

    #region CourtBailiff Helper Methods

    /// <summary>
    /// Utworzenie osoby fizycznej z flagą komornika (IsBailiff = true).
    /// </summary>
    /// <param name="nip">NIP osoby fizycznej</param>
    /// <param name="pesel">PESEL osoby fizycznej</param>
    /// <returns></returns>
    private async Task CreateCourtBailiffAsync(string nip, string pesel)
    {
        PersonCreateRequest createRequest = new PersonCreateRequest
        {
            Nip = nip,
            Pesel = pesel,
            IsBailiff = true,
            Description = "E2E CourtBailiff (osoba fizyczna z flagą komornika)"
        };

        await TestDataClient.CreatePersonAsync(createRequest);
    }

    /// <summary>
    /// Usunięcie osoby fizycznej z flagą komornika.
    /// </summary>
    /// <param name="nip">NIP osoby fizycznej</param>
    /// <returns></returns>
    private async Task RemoveCourtBailiffAsync(string nip)
    {
        PersonRemoveRequest removeRequest = new PersonRemoveRequest
        {
            Nip = nip
        };

        await TestDataClient.RemovePersonAsync(removeRequest);
    }

    #endregion

    #region EnforcementAuthority Helper Methods

    /// <summary>
    /// Stworzenie podmiotu typu EnforcementAuthority.
    /// </summary>
    /// <param name="nip"></param>
    /// <returns></returns>
    private async Task CreateEnforcementAuthorityAsync(string nip)
    {
        SubjectCreateRequest createRequest = new SubjectCreateRequest
        {
            SubjectNip = nip,
            SubjectType = SubjectType.EnforcementAuthority,
            Description = "E2E EnforcementAuthority"
        };

        await TestDataClient.CreateSubjectAsync(createRequest);
    }

    /// <summary>
    /// Usunięcie podmiotu typu EnforcementAuthority.
    /// </summary>
    /// <param name="nip"></param>
    /// <returns></returns>
    private async Task RemoveEnforcementAuthorityAsync(string nip)
    {
        SubjectRemoveRequest removeRequest = new SubjectRemoveRequest
        {
            SubjectNip = nip
        };

        await TestDataClient.RemoveSubjectAsync(removeRequest);
    }

    #endregion

    #region Shared Helper Methods

    /// <summary>
    /// Przyznanie osobie uprawnień typu EnforcementOperations.
    /// </summary>
    /// <param name="subjectNip"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    private async Task<OperationResponse> GrantEnforcementOperationsAsync(string subjectNip, string accessToken)
    {
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                Value = subjectNip
            })
            .WithPermissions(PersonPermissionType.EnforcementOperations)
            .WithDescription(PermissionDescription)
            .Build();

        OperationResponse response = await KsefClient.GrantsPermissionPersonAsync(request, accessToken, CancellationToken);
        return response;
    }

    /// <summary>
    /// Wyszukanie nadanych uprawnień osób o typie EnforcementOperations.
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    private async Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            PermissionTypes = new List<PersonPermissionType>
            {
                PersonPermissionType.EnforcementOperations
            }
        };

        PagedPermissionsResponse<PersonPermission> response = await KsefClient
            .SearchGrantedPersonPermissionsAsync(query, accessToken, pageOffset: 0, pageSize: 10, CancellationToken);

        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    /// <param name="grantedPermissions"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<PersonPermission> grantedPermissions, string accessToken)
    {
        List<OperationResponse> revokeResponses = new();

        foreach (PersonPermission permission in grantedPermissions)
        {
            OperationResponse response = await KsefClient
                .RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        List<PermissionsOperationStatusResponse> statuses = new();
        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status = await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken),
                condition: s => s is not null && s.Status is not null && s.Status.Code == OperationStatusCodeResponse.Success,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        return statuses;
    }

    #endregion
}