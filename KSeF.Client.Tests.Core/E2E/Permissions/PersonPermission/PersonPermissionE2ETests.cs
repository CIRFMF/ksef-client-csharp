using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonPermissionE2ETests : TestBase
{
    private const string PermissionDescription = "E2E test grant";

    private string accessToken = string.Empty;
    private GrantPermissionsPersonSubjectIdentifier Person { get; } = new();

    public PersonPermissionE2ETests()
    {
        // Arrange: uwierzytelnienie i przygotowanie danych testowych
        Client.Core.Models.Authorization.AuthenticationOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = auth.AccessToken.Token;

        // Ustaw dane osoby testowej (PESEL)
        Person.Value = MiscellaneousUtils.GetRandomPesel();
        Person.Type = GrantPermissionsPersonSubjectIdentifierType.Pesel;
    }

    /// <summary>
    /// Testy E2E nadawania i cofania uprawnień dla osób:
    /// - nadanie uprawnień
    /// - wyszukanie nadanych uprawnień
    /// - cofnięcie uprawnień
    /// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
    /// </summary>
    [Fact]
    public async Task PersonPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // Arrange: dane wejściowe i oczekiwane typy uprawnień
        string description = PermissionDescription;

        // Act: nadaj uprawnienia dla osoby
        OperationResponse grantResponse =
            await GrantPersonPermissionsAsync(Person, description, accessToken);

        // Assert: weryfikacja poprawności odpowiedzi operacji nadania
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // Act: odpytywanie do momentu, aż nadane uprawnienia będą widoczne (obie pule: Read i Write)
        PagedPermissionsResponse<PersonPermission> searchAfterGrant =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                (Func<PagedPermissionsResponse<PersonPermission>, bool>)(                result =>
                {
                    if (result is null || result.Permissions is null) return false;

                    List<PersonPermission> byDescription =
                        result.Permissions.Where(p => p.Description == description).ToList();

                    bool hasRead = byDescription.Any((Func<PersonPermission, bool>)(x => Enum.Parse<Client.Core.Models.Permissions.Person.PersonPermissionType>(x.PermissionScope) == Client.Core.Models.Permissions.Person.PersonPermissionType.InvoiceRead));
                    bool hasWrite = byDescription.Any((Func<PersonPermission, bool>)(x => Enum.Parse<Client.Core.Models.Permissions.Person.PersonPermissionType>(x.PermissionScope) == Client.Core.Models.Permissions.Person.PersonPermissionType.InvoiceWrite));

                    return byDescription.Count > 0 && hasRead && hasWrite;
                }),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Assert: upewnij się, że uprawnienia są widoczne i zawierają oczekiwane zakresy
        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);

        List<PersonPermission> grantedNow =
            searchAfterGrant.Permissions
                .Where(p => p.Description == description)
                .ToList();

        Assert.NotEmpty(grantedNow);
        Assert.Contains(grantedNow, x => Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.InvoiceRead);
        Assert.Contains(grantedNow, x => Enum.Parse<PersonPermissionType>(x.PermissionScope) == PersonPermissionType.InvoiceWrite);

        // Act: cofnij nadane uprawnienia
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(searchAfterGrant.Permissions, accessToken);

        // Assert: weryfikacja wyników cofania
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(searchAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationStatusCodeResponse.Success,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // Act: odpytywanie do momentu, aż uprawnienia o danym opisie znikną
        PagedPermissionsResponse<PersonPermission> searchAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result =>
                {
                    if (result is null || result.Permissions is null) return false;

                    List<PersonPermission> remainingLocal =
                        result.Permissions.Where(p => p.Description == description).ToList();

                    return remainingLocal.Count == 0;
                },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Assert: upewnij się, że nie pozostały wpisy z danym opisem
        Assert.NotNull(searchAfterRevoke);

        List<PersonPermission> remaining =
            searchAfterRevoke.Permissions
                .Where(p => p.Description == description)
                .ToList();

        Assert.Empty(remaining);
    }

    /// <summary>
    /// Nadaje uprawnienia dla osoby i zwraca odpowiedź operacji.
    /// </summary>
    private async Task<OperationResponse> GrantPersonPermissionsAsync(
        GrantPermissionsPersonSubjectIdentifier subject,
        string description,
        string accessToken)
    {
        // Arrange: zbudowanie żądania nadania uprawnień
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite)
            .WithDescription(description)
            .Build();

        // Act: wywołanie API nadawania uprawnień
        OperationResponse response =
            await KsefClient.GrantsPermissionPersonAsync(request, accessToken, CancellationToken);

        // Assert: zwrócenie odpowiedzi (asercje są wykonywane w teście)
        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        // Arrange: budowa zapytania wyszukującego uprawnienia
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            PermissionTypes = new List<PersonPermissionType>
            {
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite
            }
        };

        // Act: wywołanie API wyszukiwania
        PagedPermissionsResponse<PersonPermission> response =
            await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken, pageOffset: 0, pageSize: 10, CancellationToken);

        // Assert: zwrócenie wyniku wyszukiwania
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<PersonPermission> grantedPermissions,
        string accessToken)
    {
        // Arrange: lista odpowiedzi z operacji cofania
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        // Act: uruchomienie operacji cofania dla każdej pozycji
        foreach (PersonPermission permission in grantedPermissions)
        {
            OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Act: odpytywanie statusów do skutku (sukces) i zebranie wyników
        List<PermissionsOperationStatusResponse> statuses = new List<PermissionsOperationStatusResponse>();

        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken),
                    result => result is not null && result.Status is not null && result.Status.Code == OperationStatusCodeResponse.Success,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        // Assert: zwrócenie statusów (asercje w teście nadrzędnym)
        return statuses;
    }
}