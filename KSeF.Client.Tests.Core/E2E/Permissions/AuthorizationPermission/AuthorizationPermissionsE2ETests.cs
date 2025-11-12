using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermissions;

[Collection("AuthorizationPermissionsScenarioE2ECollection")]
public class AuthorizationPermissionsE2ETests : TestBase
{
    private readonly AuthorizationPermissionsScenarioE2EFixture _fixture;

    private const int OperationSuccessfulStatusCode = 200;
    private string accessToken = string.Empty;

    public AuthorizationPermissionsE2ETests(AuthorizationPermissionsScenarioE2EFixture fixture)
    {
        _fixture = fixture;
        AuthenticationOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService).GetAwaiter().GetResult();
        accessToken = authOperationStatusResponse.AccessToken.Token;
        _fixture.SubjectIdentifier.Value = MiscellaneousUtils.GetRandomNip();
    }

    /// <summary>
    /// Nadaje uprawnienia, wyszukuje czy zostały nadane, odwołuje uprawnienia i sprawdza, czy po odwołaniu uprawnienia już nie występują.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AuthorizationPermissions_E2E_GrantSearchRevokeSearch()
    {
        #region Nadaj uprawnienia
        // Act
        OperationResponse operationResponse = await GrantPermissionsAsync();
        _fixture.GrantResponse = operationResponse;

        // Assert
        Assert.NotNull(_fixture.GrantResponse);
        Assert.True(!string.IsNullOrEmpty(_fixture.GrantResponse.ReferenceNumber));
        #endregion

        #region Wyszukaj — powinny się pojawić (polling)
        // Act: poll aż pojawią się nadane uprawnienia
        PagedAuthorizationsResponse<AuthorizationGrant> entityRolesPaged =
            await AsyncPollingUtils.PollAsync(
                SearchGrantedRolesAsync,
                result => result is not null && result.AuthorizationGrants is { Count: > 0 },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        // Assert
        Assert.NotNull(entityRolesPaged);
        Assert.NotEmpty(entityRolesPaged.AuthorizationGrants);
        _fixture.SearchResponse = entityRolesPaged;
        #endregion

        #region Cofnij uprawnienia
        // Act
        await RevokePermissionsAsync();
        Assert.NotNull(_fixture.RevokeStatusResults);
        Assert.NotEmpty(_fixture.RevokeStatusResults);
        Assert.Equal(_fixture.RevokeStatusResults.Count, _fixture.SearchResponse.AuthorizationGrants.Count);
        Assert.All(_fixture.RevokeStatusResults, r =>
            Assert.True(r.Status.Code == OperationSuccessfulStatusCode,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );
        #endregion

        #region Wyszukaj ponownie — nie powinno być wpisów (polling)
        PagedAuthorizationsResponse<AuthorizationGrant> entityRolesAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedRolesAsync(),
                result => result is not null && result.AuthorizationGrants is { Count: 0 },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        _fixture.SearchResponse = entityRolesAfterRevoke;

        // Assert
        Assert.NotNull(_fixture.SearchResponse);
        Assert.Empty(_fixture.SearchResponse.AuthorizationGrants);
        #endregion
    }

    /// <summary>
    /// Nadaje uprawnienia.
    /// </summary>
    /// <returns>Numer referencyjny operacji</returns>
    private async Task<OperationResponse> GrantPermissionsAsync()
    {
        GrantPermissionsAuthorizationRequest grantPermissionAuthorizationRequest =
            GrantAuthorizationPermissionsRequestBuilder
            .Create()
            .WithSubject(_fixture.SubjectIdentifier)
            .WithPermission(AuthorizationPermissionType.SelfInvoicing)
            .WithDescription("E2E test grant")
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsAuthorizationPermissionAsync(grantPermissionAuthorizationRequest,
            accessToken, CancellationToken);

        return operationResponse;
    }

    /// <summary>
    /// Wyszukuje uprawnienia.
    /// </summary>
    /// <returns>Stronicowana lista nadanych uprawnień.</returns>
    private async Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchGrantedRolesAsync()
    {
        EntityAuthorizationsQueryRequest request = new EntityAuthorizationsQueryRequest();
        PagedAuthorizationsResponse<AuthorizationGrant> entityRolesPaged = await KsefClient
            .SearchEntityAuthorizationGrantsAsync(
                request,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken
            );

        return entityRolesPaged;
    }

    /// <summary>
    /// Odwołuje uprawnienia.
    /// </summary>
    private async Task RevokePermissionsAsync()
    {
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (AuthorizationGrant permission in _fixture.SearchResponse.AuthorizationGrants)
        {
            OperationResponse resp = await KsefClient.RevokeAuthorizationsPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(resp);
        }

        // Sprawdzenie statusów wszystkich operacji (polling do 200)
        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken),
                    result => result.Status.Code == OperationSuccessfulStatusCode,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 30,
                    cancellationToken: CancellationToken);

            _fixture.RevokeStatusResults.Add(status);
        }
    }
}