using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonPermissions_OwnerNip_MyPermissions_E2ETests : TestBase
{
    private IPersonTokenService _tokenService => _scope.ServiceProvider.GetRequiredService<IPersonTokenService>();

    /// <summary>
    /// E2E: „Moje uprawnienia” właściciela w kontekście NIP.
    /// Lista może być pusta, ale w access tokenie pole „per” powinno zawierać „Owner” (właściciel).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Autoryzacja właściciela w kontekście NIP.</description></item>
    /// <item><description>Zapytanie o „moje uprawnienia” (lista może być pusta).</description></item>
    /// <item><description>W AccessToken w claimie „per” musi być „Owner”.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task Search_MyPermissions_AsOwnerNip_InCurrentContext_ShouldReturnPage_AndTokenShouldContainOwner()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        PersonPermissionsQueryRequest request = new PersonPermissionsQueryRequest
        {
            ContextIdentifier = new PersonPermissionsContextIdentifier
            {
                Type = PersonPermissionsContextIdentifierType.Nip,
                Value = ownerNip
            },
            TargetIdentifier = new PersonPermissionsTargetIdentifier
            {
                Type = PersonPermissionsTargetIdentifierType.Nip,
                Value = ownerNip
            },
            PermissionState = PersonPermissionState.Active,
            QueryType = PersonQueryType.PermissionsInCurrentContext
        };
        #endregion

        #region Act
        // 1) Wynik może być pusty i to jest OK dla Ownera
        PagedPermissionsResponse<PersonPermission> page =
            await KsefClient.SearchGrantedPersonPermissionsAsync(
                request, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken);

        // 2) Token – deterministyczna weryfikacja Owner w claimie „per”
        
        PersonToken token = _tokenService.MapFromJwt(ownerAccessToken);
        #endregion

        #region Assert
        // API odpowiedziało poprawnie (lista może być pusta)
        Assert.NotNull(page);
        Assert.NotNull(page.Permissions);

        // Jesteśmy właścicielem w tym kontekście NIP → w tokenie musi być "Owner"
        Assert.Equal("ContextToken", token.TokenType);
        Assert.Equal("Nip", token.ContextIdType);
        Assert.Equal(ownerNip, token.ContextIdValue);
        Assert.Contains("Owner", token.Permissions, StringComparer.OrdinalIgnoreCase);
        // (opcjonalnie) Role zmapowane łącznie również zawierają Owner
        Assert.Contains("Owner", token.Roles, StringComparer.OrdinalIgnoreCase);
        #endregion
    }
}
