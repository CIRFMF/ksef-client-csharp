using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Klient służący do wyszukiwania uprawnień.
    /// </summary>
    public interface ISearchPermissionClient
    {
        /// <summary>
        /// Pobranie listy moich uprawnień.
        /// </summary>
        /// <param name="requestPayload"><see cref="PersonalPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{PersonalPermission}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<PersonalPermission>> SearchGrantedPersonalPermissionsAsync(PersonalPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień do pracy w KSeF nadanych osobom fizycznym lub podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="PersonPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{PersonPermission}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(PersonPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień administratora podmiotu podrzędnego.
        /// </summary>
        /// <param name="requestPayload"><see cref="SubunitPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{SubunitPermission}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<SubunitPermission>> SearchSubunitAdminPermissionsAsync(SubunitPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień administratora podmiotu podrzędnego.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedRolesResponse{EntityRole}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedRolesResponse<EntityRole>> SearchEntityInvoiceRolesAsync(string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień do obsługi faktur nadanych podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="SubordinateEntityRolesQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{SubordinateEntityRole}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedRolesResponse<SubordinateEntityRole>> SearchSubordinateEntityInvoiceRolesAsync(SubordinateEntityRolesQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień o charakterze uprawnień nadanych podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="EntityAuthorizationsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedAuthorizationsResponse{AuthorizationGrant}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchEntityAuthorizationGrantsAsync(EntityAuthorizationsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień nadanych podmiotom unijnym.
        /// </summary>
        /// <param name="requestPayload"><see cref="EuEntityPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="PagedPermissionsResponse{EuEntityPermission}"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<EuEntityPermission>> SearchGrantedEuEntityPermissionsAsync(EuEntityPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień do obsługi faktur w bieżącym kontekście.
        /// </summary>
        /// <param name="requestPayload">Zapytanie z ContextIdentifier</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="EntityPermissionGrantResponse"/></returns>
        Task<EntityPermissionGrantResponse> QueryEntitiesGrantsAsync(
            EntityPermissionGrantQueryRequest requestPayload,
            string accessToken,
            int? pageOffset = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);
    }
}
