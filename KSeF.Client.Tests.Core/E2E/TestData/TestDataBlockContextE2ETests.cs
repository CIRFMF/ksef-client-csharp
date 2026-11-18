using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.TestData;

/// <summary>
/// Operacje blokowania i odblokowywania kontekstu (POST /testdata/context/block oraz /unblock)
/// dostępne wyłącznie na środowisku testowym i służą jedynie do symulacji blokady.
/// Umożliwiają przetestowanie obsługi odrzuconego uwierzytelnienia (status 480) przez integratora.
/// </summary>
public class BlockContextE2ETests : TestBase
{
    [Fact]
    public async Task BlockContext_AuthenticationFailsWith480_ThenUnblockRestoresAccess()
    {
        string nip = MiscellaneousUtils.GetRandomNip();
        ContextBlockingIdentifier context = new() { Type = ContextBlockingIdentifierType.Nip, Value = nip };

        AuthenticationOperationStatusResponse beforeBlock = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);
        Assert.False(string.IsNullOrEmpty(beforeBlock.AccessToken?.Token));

        try
        {
            await TestDataClient.BlockContextAsync(context, CancellationToken);

            KsefAuthenticationException exception = await Assert.ThrowsAsync<KsefAuthenticationException>(
                () => AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip));
            Assert.Equal(AuthenticationStatusCodeResponse.AuthenticationBlocked, exception.StatusCode);
        }
        finally
        {
            await TestDataClient.UnblockContextAsync(context, CancellationToken);
        }

        AuthenticationOperationStatusResponse afterUnblock = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);
        Assert.False(string.IsNullOrEmpty(afterUnblock.AccessToken?.Token));
    }
}
