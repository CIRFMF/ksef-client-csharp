using System.Text;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Http.Helpers;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class SearchPermissionClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), ISearchPermissionClient
{
    private static string WithPagination(string endpoint, int? pageOffset, int? pageSize)
    {
        StringBuilder sb = new(endpoint);
        PaginationHelper.AppendPagination(pageOffset, pageSize, sb);
        return sb.ToString();
    }

    /// <inheritdoc />
    public Task<PagedPermissionsResponse<PersonalPermission>> SearchGrantedPersonalPermissionsAsync(
        PersonalPermissionsQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.PersonalGrants, pageOffset, pageSize);
        return ExecuteAsync<PagedPermissionsResponse<PersonalPermission>, PersonalPermissionsQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(
        PersonPermissionsQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.PersonsGrants, pageOffset, pageSize);
        return ExecuteAsync<PagedPermissionsResponse<PersonPermission>, PersonPermissionsQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedPermissionsResponse<SubunitPermission>> SearchSubunitAdminPermissionsAsync(
        SubunitPermissionsQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.SubunitsGrants, pageOffset, pageSize);
        return ExecuteAsync<PagedPermissionsResponse<SubunitPermission>, SubunitPermissionsQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedRolesResponse<EntityRole>> SearchEntityInvoiceRolesAsync(
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.EntitiesRoles, pageOffset, pageSize);
        return ExecuteAsync<PagedRolesResponse<EntityRole>>(endpoint, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedRolesResponse<SubordinateEntityRole>> SearchSubordinateEntityInvoiceRolesAsync(
        SubordinateEntityRolesQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.SubordinateEntitiesRoles, pageOffset, pageSize);
        return ExecuteAsync<PagedRolesResponse<SubordinateEntityRole>, SubordinateEntityRolesQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchEntityAuthorizationGrantsAsync(
        EntityAuthorizationsQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.AuthorizationsGrants, pageOffset, pageSize);
        return ExecuteAsync<PagedAuthorizationsResponse<AuthorizationGrant>, EntityAuthorizationsQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedPermissionsResponse<EuEntityPermission>> SearchGrantedEuEntityPermissionsAsync(
        EuEntityPermissionsQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = WithPagination(Routes.Permissions.Query.EuEntitiesGrants, pageOffset, pageSize);
        return ExecuteAsync<PagedPermissionsResponse<EuEntityPermission>, EuEntityPermissionsQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }

    public Task<EntityPermissionGrantResponse> QueryEntitiesGrantsAsync(
        EntityPermissionGrantQueryRequest requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        string endpoint = WithPagination(Routes.Permissions.Query.Entities, pageOffset, pageSize);
        return ExecuteAsync<EntityPermissionGrantResponse, EntityPermissionGrantQueryRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken);
    }
}
