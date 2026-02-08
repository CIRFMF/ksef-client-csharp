#if !NETFRAMEWORK
using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.ApiResponses;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;

/// <summary>
/// Testy E2E nadawania i cofania uprawnień podmiotom:
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
    private readonly string accessToken = string.Empty;
    private GrantPermissionsEntitySubjectIdentifier Entity { get; } = new();

    public EntityPermissionE2ETests()
    {
        AuthenticationOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient)
            .GetAwaiter().GetResult();

        accessToken = authOperationStatusResponse.AccessToken.Token;
        Entity.Value = MiscellaneousUtils.GetRandomNip();
        Entity.Type = GrantPermissionsEntitySubjectIdentifierType.Nip;
    }

    [Fact]
    public async Task EntityPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // 1) Nadaj uprawnienia podmiotowi
        OperationResponse grantResponse = await GrantEntityPermissionsAsync(Entity, PermissionDescription, accessToken);
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // 2) Wyszukaj nadane uprawnienia — poll aż będą widoczne i kompletne
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterGrant =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken).ConfigureAwait(false),
                result =>
                    result is not null
                    && result.Permissions is { Count: > 0 }
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == true &&
                        Enum.Parse<EntityStandardPermissionType>(x.PermissionScope.ToString()) == EntityStandardPermissionType.InvoiceRead)
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == false &&
                        Enum.Parse<EntityStandardPermissionType>(x.PermissionScope.ToString()) == EntityStandardPermissionType.InvoiceWrite),
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.True(searchAfterGrant.Permissions.All(x => x.Description == PermissionDescription));
        Assert.True(searchAfterGrant.Permissions.First(x =>
            x.CanDelegate == true &&
            x.PermissionScope.ToString() == EntityStandardPermissionType.InvoiceRead.ToString()) is not null);

        Assert.True(searchAfterGrant.Permissions.First(x =>
            x.CanDelegate == false &&
            x.PermissionScope.ToString() == EntityStandardPermissionType.InvoiceWrite.ToString()) is not null);

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

        // 4) Ponowne wyszukiwanie uprawnień
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken).ConfigureAwait(false),
                result => result is not null && result.Permissions is { Count: 0 },
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterRevoke);
        Assert.Empty(searchAfterRevoke.Permissions);
    }

    /// <summary>
    /// Nadaje uprawnienia podmiotowi i zwraca numer referencyjny operacji.
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
                Client.Core.Models.Permissions.Entity.EntityPermission.New(EntityStandardPermissionType.InvoiceRead, true),
                Client.Core.Models.Permissions.Entity.EntityPermission.New(EntityStandardPermissionType.InvoiceWrite, false)
            )
            .WithDescription(description)
            .WithSubjectDetails(new PermissionsEntitySubjectDetails
            {
                FullName = $"Entity {subject.Value}"
            })
            .Build();

        OperationResponse response = await KsefClient.GrantsPermissionEntityAsync(request, accessToken, CancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane osobom i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        PersonPermissionsQueryRequest query = new();
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> response = await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<Client.Core.Models.Permissions.PersonPermission> grantedPermissions,
        string accessToken)
    {
        List<OperationResponse> revokeResponses = [];

        // Uruchomienie operacji cofania
        foreach (Client.Core.Models.Permissions.PersonPermission permission in grantedPermissions)
        {
            OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None).ConfigureAwait(false);
            revokeResponses.Add(response);
        }

        // Sprawdzenie statusów wszystkich operacji — polling do 200
        List<PermissionsOperationStatusResponse> statuses = [];

        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken).ConfigureAwait(false),
                    result => result.Status.Code == OperationStatusCodeResponse.Success,
                    cancellationToken: CancellationToken).ConfigureAwait(false);

            statuses.Add(status);
        }

        return statuses;
    }
}
#endif