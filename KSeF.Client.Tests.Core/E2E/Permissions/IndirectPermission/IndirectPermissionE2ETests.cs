using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.IndirectPermissions;

/// <summary>
/// Testy end-to-end dla nadawania uprawnień w sposób pośredni systemie KSeF.
/// Obejmuje scenariusze nadawania i odwoływania uprawnień oraz ich weryfikację.
/// </summary>
[Collection("IndirectPermissionScenario")]
public class IndirectPermissionE2ETests : TestBase
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(SleepTime);

    private string ownerAccessToken { get; set; }
    private string ownerNip { get; set; }

    private string delegateAccessToken { get; set; }
    private string delegateNip { get; set; }

    private IndirectEntitySubjectIdentifier Subject { get; } =
        new IndirectEntitySubjectIdentifier
        {
            Type = IndirectEntitySubjectIdentifierType.Nip
        };


    public IndirectPermissionE2ETests()
    {
        ownerNip = MiscellaneousUtils.GetRandomNip();
        delegateNip = MiscellaneousUtils.GetRandomNip();
        Subject.Value = MiscellaneousUtils.GetRandomNip();
    }

    /// <summary>
    /// Wykonuje kompletny scenariusz obsługi uprawnień pośrednich E2E:
    /// 1. Nadaje uprawnienia CredentialsManage dla pośrednika
    /// 2. Nadaje uprawnienia pośrednie
    /// 3. Wyszukuje nadane uprawnienia
    /// 4. Usuwa uprawnienia
    /// 5. Sprawdza, że uprawnienia zostały poprawnie usunięte
    /// </summary>
    [Fact]
    public async Task IndirectPermission_E2E_GrantSearchRevokeSearch()
    {
        // Arrange: Uwierzytelnienie właściciela 
        ownerAccessToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

        // Act: 1) Nadanie uprawnień CredentialsManage dla pośrednika
        PermissionsOperationStatusResponse personGrantStatus = await GrantCredentialsManageToDelegateAsync();

        // Assert
        Assert.NotNull(personGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, personGrantStatus.Status.Code);

        // Arrange: Uwierzytelnienie pośrednika (Arrange)
        delegateAccessToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, delegateNip)).AccessToken.Token;

        // Act: 2) Nadanie uprawnień pośrednich przez pośrednika
        PermissionsOperationStatusResponse indirectGrantStatus = await GrantIndirectPermissionsAsync();

        // Assert
        Assert.NotNull(indirectGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, indirectGrantStatus.Status.Code);

        // Act: 3) Wyszukanie nadanych uprawnień (w bieżącym kontekście, nieaktywne)
        PagedPermissionsResponse<PersonPermission> permissionsAfterGrant =
            await SearchGrantedPersonPermissionsInCurrentContextAsync(
                accessToken: delegateAccessToken,
                includeInactive: true,
                pageOffset: 0,
                pageSize: 10);

        // Assert
        Assert.NotNull(permissionsAfterGrant);
        Assert.NotEmpty(permissionsAfterGrant.Permissions);

        // Poll: upewnij się, że uprawnienia są widoczne/ustabilizowane przed cofnięciem
        permissionsAfterGrant = await AsyncPollingUtils.PollAsync(
            action: () => SearchGrantedPersonPermissionsInCurrentContextAsync(
                accessToken: delegateAccessToken,
                includeInactive: true,
                pageOffset: 0,
                pageSize: 10),
            condition: r => r.Permissions is { Count: > 0 },
            delay: PollDelay,
            maxAttempts: 30,
            cancellationToken: CancellationToken
        );

        // Act: 4) Cofnięcie nadanych uprawnień
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(permissionsAfterGrant.Permissions, delegateAccessToken);

        // Assert
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(permissionsAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationStatusCodeResponse.Success,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // Poll: 5) Wyszukanie po cofnięciu – oczekujemy pustej listy
        PagedPermissionsResponse<PersonPermission> permissionsAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                action: () => SearchGrantedPersonPermissionsInCurrentContextAsync(
                    accessToken: delegateAccessToken,
                    includeInactive: true,
                    pageOffset: 0,
                    pageSize: 10),
                condition: r => r.Permissions is { Count: 0 },
                delay: PollDelay,
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

        // Assert
        Assert.NotNull(permissionsAfterRevoke);
        Assert.Empty(permissionsAfterRevoke.Permissions);
    }

    /// <summary>
    /// Nadaje uprawnienie CredentialsManage przez właściciela dla pośrednika
    /// </summary>
    /// <returns>Status operacji nadania uprawnień osobowych</returns>
    private async Task<PermissionsOperationStatusResponse> GrantCredentialsManageToDelegateAsync()
    {
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(
                new GrantPermissionsPersonSubjectIdentifier
                {
                    Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                    Value = delegateNip
                }
            )
            .WithPermissions(PersonPermissionType.CredentialsManage)
            .WithDescription("E2E test - nadanie uprawnień CredentialsManage do zarządzania uprawnieniami")
            .Build();

        OperationResponse grantOperationResponse =
            await KsefClient.GrantsPermissionPersonAsync(request, ownerAccessToken, CancellationToken);

        Assert.NotNull(grantOperationResponse);
        Assert.False(string.IsNullOrEmpty(grantOperationResponse.ReferenceNumber));

        // Poll zamiast stałego opóźnienia
        PermissionsOperationStatusResponse grantOperationStatus =
            await WaitForOperationSuccessAsync(grantOperationResponse.ReferenceNumber, ownerAccessToken);

        return grantOperationStatus;
    }

    /// <summary>
    /// Nadaje uprawnienia pośrednie dla podmiotu w kontekście wskazanego NIP przez pośrednika.
    /// </summary>
    /// <returns>Status operacji nadania uprawnień pośrednich</returns>
    private async Task<PermissionsOperationStatusResponse> GrantIndirectPermissionsAsync()
    {
        GrantPermissionsIndirectEntityRequest request = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(Subject)
            .WithContext(
                new IndirectEntityTargetIdentifier
                {
                    Type = IndirectEntityTargetIdentifierType.Nip,
                    Value = ownerNip
                }
            )
            .WithPermissions(
                IndirectEntityStandardPermissionType.InvoiceRead,
                IndirectEntityStandardPermissionType.InvoiceWrite
            )
            .WithDescription("E2E test - przekazanie uprawnień (InvoiceRead, InvoiceWrite) przez pośrednika")
            .Build();

        OperationResponse grantOperationResponse =
            await KsefClient.GrantsPermissionIndirectEntityAsync(request, delegateAccessToken, CancellationToken);

        Assert.NotNull(grantOperationResponse);
        Assert.False(string.IsNullOrEmpty(grantOperationResponse.ReferenceNumber));

        // Poll zamiast stałego opóźnienia
        PermissionsOperationStatusResponse grantOperationStatus =
            await WaitForOperationSuccessAsync(grantOperationResponse.ReferenceNumber, delegateAccessToken);

        return grantOperationStatus;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane osobom w bieżącym kontekście.
    /// Możliwe jest włączenie filtracji po stanie (np. nieaktywne).
    /// </summary>
    /// <returns>Zwraca stronicowaną listę uprawnień.</returns>
    private async Task<PagedPermissionsResponse<PersonPermission>>
        SearchGrantedPersonPermissionsInCurrentContextAsync(
            string accessToken,
            bool includeInactive,
            int pageOffset,
            int pageSize)
    {
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            QueryType = PersonQueryType.PermissionsGrantedInCurrentContext,
            PermissionState = includeInactive
                ? PersonPermissionState.Inactive
                : PersonPermissionState.Active
        };

        PagedPermissionsResponse<PersonPermission> pagedPermissionsResponse = await KsefClient.SearchGrantedPersonPermissionsAsync(
            query,
            accessToken,
            pageOffset: pageOffset,
            pageSize: pageSize,
            CancellationToken);
        return pagedPermissionsResponse;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<PersonPermission> permissions,
        string accessToken)
    {
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (PersonPermission permission in permissions)
        {
            OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Poll statusów wszystkich operacji (równolegle)
        Task<PermissionsOperationStatusResponse>[] revokeStatusTasks = revokeResponses
            .Select(r => WaitForOperationSuccessAsync(r.ReferenceNumber, accessToken))
            .ToArray();

        PermissionsOperationStatusResponse[] revokeStatusResults =
            await Task.WhenAll(revokeStatusTasks);

        return revokeStatusResults.ToList();
    }

    /// <summary>
    /// Czeka aż status operacji będzie pomyślny (200) z wykorzystaniem pollingu.
    /// </summary>
    private Task<PermissionsOperationStatusResponse> WaitForOperationSuccessAsync(
        string operationReferenceNumber,
        string accessToken)
        => AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(operationReferenceNumber, accessToken),
            condition: r => r.Status.Code == OperationStatusCodeResponse.Success,
            delay: PollDelay,
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );
}