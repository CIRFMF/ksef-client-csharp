using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.ApiResponses;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermissions;

/// <summary>
/// Testy E2E nadawania i cofania uprawnień dla podmiotów:
/// - nadanie uprawnień
/// - wyszukanie nadanych uprawnień
/// - cofnięcie uprawnień
/// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
/// </summary>
[Collection("EntityPermissionScenario")]
public partial class EntityPermissionE2ETests : TestBase
{
    private const string PermissionDescription = "E2E test grant";

    // Zamiast fixture: prywatne readonly pola
    private string accessToken = string.Empty;
    private GrantPermissionsEntitySubjectIdentifier Entity { get; } = new();

    public EntityPermissionE2ETests()
    {
        AuthenticationOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = authOperationStatusResponse.AccessToken.Token;
        Entity.Value = MiscellaneousUtils.GetRandomNip();
        Entity.Type = GrantPermissionsEntitySubjectIdentifierType.Nip;
    }

    [Fact]
    public async Task EntityPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // 1) Nadaj uprawnienia dla podmiotu
        OperationResponse grantResponse = await GrantEntityPermissionsAsync(Entity, PermissionDescription, accessToken);
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // 2) Wyszukaj nadane uprawnienia — poll aż będą widoczne i kompletne
        PagedPermissionsResponse<PersonPermission> searchAfterGrant =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result =>
                    result is not null
                    && result.Permissions is { Count: > 0 }
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == true &&
                        Enum.Parse<EntityStandardPermissionType>(x.PermissionScope) == EntityStandardPermissionType.InvoiceRead)
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == false &&
                        Enum.Parse<EntityStandardPermissionType>(x.PermissionScope) == EntityStandardPermissionType.InvoiceWrite),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.True(searchAfterGrant.Permissions.All(x => x.Description == PermissionDescription));
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == true && Enum.Parse<EntityStandardPermissionType>(x.PermissionScope) == EntityStandardPermissionType.InvoiceRead) is not null);
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == false && Enum.Parse<EntityStandardPermissionType>(x.PermissionScope) == EntityStandardPermissionType.InvoiceWrite) is not null);


        // 2a) Odczytaj listę ról podmiotu bieżącego kontekstu logowania
        PagedRolesResponse<EntityRole> searchEntityInvoiceRoles = await KsefClient.SearchEntityInvoiceRolesAsync(accessToken);

        Assert.NotNull(searchEntityInvoiceRoles);
        
        // 3) Cofnij nadane uprawnienia (ze sprawdzeniem statusów przez polling)
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(searchAfterGrant.Permissions, accessToken);
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(searchAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationStatusCodeResponse.Success,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // 4) Wyszukaj ponownie — poll aż lista będzie pusta
        PagedPermissionsResponse<PersonPermission> searchAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result => result is not null && result.Permissions is { Count: 0 },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterRevoke);
        Assert.Empty(searchAfterRevoke.Permissions);
    }

    /// <summary>
    /// Nadaje uprawnienia dla podmiotu i zwraca numer referencyjny operacji.
    /// </summary>
    private async Task<OperationResponse> GrantEntityPermissionsAsync(
        GrantPermissionsEntitySubjectIdentifier subject,
        string description,
        string accessToken)
    {
        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                EntityPermission.New(EntityStandardPermissionType.InvoiceRead, true),
                EntityPermission.New(EntityStandardPermissionType.InvoiceWrite, false)
            )
            .WithDescription(description)
            .Build();

        OperationResponse response = await KsefClient.GrantsPermissionEntityAsync(request, accessToken, CancellationToken);
        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest();
        PagedPermissionsResponse<PersonPermission> response = await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken);
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<PersonPermission> grantedPermissions,
        string accessToken)
    {
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (PersonPermission permission in grantedPermissions)
        {
            OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Sprawdzenie statusów wszystkich operacji — polling do 200
        List<PermissionsOperationStatusResponse> statuses = new List<PermissionsOperationStatusResponse>();

        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken),
                    result => result.Status.Code == OperationStatusCodeResponse.Success,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 30,
                    cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        return statuses;
    }
}