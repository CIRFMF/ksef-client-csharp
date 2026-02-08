using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Permissions;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class PermissionOperationClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IPermissionOperationClient
{
    /// <inheritdoc />
    public Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(operationReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Permissions.Operations.ByReference(Uri.EscapeDataString(operationReferenceNumber));
        return ExecuteAsync<PermissionsOperationStatusResponse>(endpoint, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PermissionsAttachmentAllowedResponse> GetAttachmentPermissionStatusAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        return ExecuteAsync<PermissionsAttachmentAllowedResponse>(Routes.Permissions.Attachments.Status, HttpMethod.Get, accessToken, cancellationToken);
    }
}
