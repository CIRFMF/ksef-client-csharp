using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;

namespace KSeF.Client.Clients;

/// <summary>
/// Klient służący do nadawania uprawnień.
/// </summary>
public class GrantPermissionClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IGrantPermissionClient
{
    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionPersonAsync(GrantPermissionsPersonRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsPersonRequest>(
            Routes.Permissions.Grants.Persons,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionEntityAsync(GrantPermissionsEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsEntityRequest>(
            Routes.Permissions.Grants.Entities,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsAuthorizationPermissionAsync(GrantPermissionsAuthorizationRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsAuthorizationRequest>(
            Routes.Permissions.Grants.Authorizations,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionIndirectEntityAsync(GrantPermissionsIndirectEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsIndirectEntityRequest>(
            Routes.Permissions.Grants.Indirect,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionSubUnitAsync(GrantPermissionsSubunitRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsSubunitRequest>(
            Routes.Permissions.Grants.Subunits,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionEUEntityAsync(GrantPermissionsEuEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsEuEntityRequest>(
            Routes.Permissions.Grants.EuEntities,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync(GrantPermissionsEuEntityRepresentativeRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OperationResponse, GrantPermissionsEuEntityRepresentativeRequest>(
            Routes.Permissions.Grants.EuEntitiesRepresentatives,
            requestPayload,
            accessToken,
            cancellationToken);
    }
}
