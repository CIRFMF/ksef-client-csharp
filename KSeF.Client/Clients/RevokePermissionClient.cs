using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class RevokePermissionClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IRevokePermissionClient
{
    /// <inheritdoc />
    public Task<OperationResponse> RevokeCommonPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(permissionId);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Permissions.Common.GrantById(Uri.EscapeDataString(permissionId));
        return ExecuteAsync<OperationResponse>(endpoint, HttpMethod.Delete, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> RevokeAuthorizationsPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(permissionId);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Permissions.Authorizations.GrantById(Uri.EscapeDataString(permissionId));
        return ExecuteAsync<OperationResponse>(endpoint, HttpMethod.Delete, accessToken, cancellationToken);
    }
}
