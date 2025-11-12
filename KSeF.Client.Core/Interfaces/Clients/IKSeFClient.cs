
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Peppol;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Core.Models;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using System;

namespace KSeF.Client.Core.Interfaces.Clients
{
    public interface IKSeFClient : IActiveSessionsClient, IAuthorizationClient
    {
        

        /// <param name="cancellationToken">Cancellation token.</param>
        /// <summary>
        /// Otwarcie sesji interaktywnej
        /// </summary>
        /// <remarks>
        /// Inicjalizacja wysyłki interaktywnej faktur.
        /// </remarks>
        /// <param name="accessToken">Access token.</param>
        /// <param name="requestPayload"><see cref="OpenOnlineSessionRequest"/></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="OpenOnlineSessionResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Wysłanie faktury
        /// </summary>
        /// <remarks>
        /// Przyjmuje zaszyfrowaną fakturę oraz jej metadane i rozpoczyna jej przetwarzanie.
        /// </remarks>
        /// <param name="requestPayload"><see cref="SendInvoiceRequest"/>Zaszyfrowana faktura wraz z metadanymi.</param>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="SendInvoiceResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zamknięcie sesji interaktywnej
        /// </summary>
        /// <remarks>
        /// Zamyka sesję interaktywną i rozpoczyna generowanie zbiorczego UPO.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="accessToken"></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ApiException">A server side error occurred.</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Otwarcie sesji wsadowej
        /// </summary>
        /// <remarks>
        /// Otwiera sesję do wysyłki wsadowej faktur.
        /// </remarks>
        /// <param name="requestPayload"><see cref=OpenBatchSessionRequest"/>schemat wysyłanych faktur, informacje o paczce faktur oraz informacje o kluczu używanym do szyfrowania.</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OpenBatchSessionResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OpenBatchSessionResponse> OpenBatchSessionAsync(OpenBatchSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zamknięcie sesji wsadowej.
        /// </summary>
        /// <remarks>
        /// Zamyka sesję wsadową, rozpoczyna procesowanie paczki faktur i generowanie UPO dla prawidłowych faktur oraz zbiorczego UPO dla sesji.
        /// </remarks>
        /// <param name="batchSessionReferenceNumber">Numer referencyjny sesji wsadowej.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task CloseBatchSessionAsync(string batchSessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca listę sesji spełniających podane kryteria wyszukiwania.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Rozmiar strony wyników.</param>
        /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns><see cref="SessionsListResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<SessionsListResponse> GetSessionsAsync(SessionType sessionType, string accessToken, int? pageSize, string continuationToken, SessionsFilter sessionsFilter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusu sesji
        /// </summary>
        /// <remarks>
        /// Zwraca informacje o statusie sesji.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token.</param>"
        /// <returns><see cref="SessionStatusResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<SessionStatusResponse> GetSessionStatusAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusów faktur sesji
        /// </summary>
        /// <remarks>
        /// Zwraca listę faktur przesłanych w sesji wraz z ich statusami, oraz informacje na temat ilości poprawnie i niepoprawnie przetworzonych faktur.
        /// </remarks>
        /// <param name="sessionReferenceNumber\">Numer referencyjny sesji</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="pageSize">Rozmiar strony wyników.</param>
        /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns><see cref="SessionInvoicesResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        /// <exception cref="ApiException">Brak uprawnień. (403 Forbidden)</exception>
        Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusu faktury z sesji
        /// </summary>
        /// <remarks>Zwraca fakturę przesłaną w sesji wraz ze statusem.</remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
        /// <param name="invoiceReferenceNumber">Numer referencyjny faktury.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns><see cref="SessionInvoice"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<SessionInvoice> GetSessionInvoiceAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie niepoprawnie przetworzonych dokumentów sesji
        /// </summary>
        /// <remarks>
        /// Zwraca listę niepoprawnie przetworzonych dokumentów przesłanych w sesji wraz z ich statusami.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Rozmiar strony wyników.</param>
        /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns><see cref="SessionInvoicesResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<SessionInvoicesResponse> GetSessionFailedInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default);

        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <summary>
        /// Pobranie UPO faktury z sesji na podstawie numeru KSeF
        /// </summary>
        /// <remarks>
        /// Zwraca UPO faktury przesłanej w sesji na podstawie jego numeru KSeF.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="ksefNumber">Numer KSeF faktury</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>UPO w formie XML</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<string> GetSessionInvoiceUpoByKsefNumberAsync(string sessionReferenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie UPO faktury z sesji na podstawie numeru referencyjnego faktury.
        /// </summary>
        /// <remarks>
        /// Zwraca UPO faktury przesłanego w sesji na podstawie jego numeru KSeF.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="invoiceReferenceNumber">Numer referencyjny faktury</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns>UPO w formie XML</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<string> GetSessionInvoiceUpoByReferenceNumberAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie UPO dla sesji
        /// </summary>
        /// <remarks>
        /// Zwraca XML zawierający zbiorcze UPO dla sesji.
        /// </remarks>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
        /// <param name="upoReferenceNumber">Numer referencyjny UPO</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns>Zbiorcze UPO w formie XML</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<string> GetSessionUpoAsync(string sessionReferenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie faktury po numerze KSeF
        /// </summary>
        /// <param name="ksefNumber">Numer KSeF faktury</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns>Faktura w formie XML.</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<string> GetInvoiceAsync(string ksefNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca listę metadanych faktur spełniające podane kryteria wyszukiwania.
        /// </summary>
        /// <param name="requestPayload"><see cref="InvoiceQueryFilters"/>zestaw filtrów</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="pageOffset">Numer strony wyników.</param>
        /// <param name="pageSize">Rozmiar strony wyników.</param>
        /// <param name="cancellationToken">Cancellation token./param>
        /// <returns><see cref="PagedInvoiceResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedInvoiceResponse> QueryInvoiceMetadataAsync(InvoiceQueryFilters requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, SortOrder sortOrder = SortOrder.Asc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusu operacji - uprawnienia
        /// </summary>
        /// <param name="operationReferenceNumber">Numer referencyjny operacji.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="PermissionsOperationStatusResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rozpoczyna asynchroniczną operację odbierania uprawnienia o podanym identyfikatorze.
        /// </summary>
        /// <param name="permissionId">Id uprawnienia.</param>
        /// <param name="accessToken">Token dostępu.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> RevokeCommonPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rozpoczyna asynchroniczną operacje odbierania uprawnienia o podanym identyfikatorze. 
        /// Ta metoda służy do odbierania uprawnień o charakterze upoważnień.
        /// </summary>
        /// <param name="permissionId">Id uprawnienia.</param>
        /// <param name="accessToken">Token dostępu.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> RevokeAuthorizationsPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sprawdzenie czy obecny kontekst posiada zgodę na wystawianie faktur z załącznikiem.
        ///Wymagane uprawnienia: CredentialsManage, CredentialsRead.
        /// </summary>    
        /// <param name="accessToken">Token dostępu.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="PermissionsAttachmentAllowedResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PermissionsAttachmentAllowedResponse> GetAttachmentPermissionStatusAsync(string accessToken, CancellationToken cancellationToken = default);


        /// <summary>
        /// Pobranie listy moich uprawnień.
        /// </summary>
        /// <param name="requestPayload"><see cref="PersonalPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{PersonalPermission}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<PersonalPermission>> SearchGrantedPersonalPermissionsAsync(PersonalPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień do pracy w KSeF nadanych osobom fizycznym lub podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="PersonPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{PersonPermission}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(PersonPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień administratora podmiotu podrzędnego.
        /// </summary>
        /// <param name="requestPayload"><see cref="SubunitPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{SubunitPermission}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<SubunitPermission>> SearchSubunitAdminPermissionsAsync(SubunitPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null,int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Pobranie listy uprawnień administratora podmiotu podrzędnego.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedRolesResponse{EntityRole>}"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedRolesResponse<EntityRole>> SearchEntityInvoiceRolesAsync(string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień do obsługi faktur nadanych podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="SubordinateEntityRolesQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedPermissionsResponse{SubordinateEntityRole}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedRolesResponse<SubordinateEntityRole>> SearchSubordinateEntityInvoiceRolesAsync(SubordinateEntityRolesQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null,  CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień o charakterze uprawnień nadanych podmiotom.
        /// </summary>
        /// <param name="requestPayload"><see cref="EntityAuthorizationsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="PagedAuthorizationsResponse{AuthorizationGrant}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchEntityAuthorizationGrantsAsync(EntityAuthorizationsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy uprawnień nadanych podmiotom unijnym.
        /// </summary>
        /// <param name="requestPayload"><see cref="EuEntityPermissionsQueryRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="PagedPermissionsResponse{EuEntityPermission}>"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<PagedPermissionsResponse<EuEntityPermission>> SearchGrantedEuEntityPermissionsAsync(EuEntityPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Nadanie osobom fizycznym uprawnień do pracy w KSeF
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsPersonRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionPersonAsync(GrantPermissionsPersonRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        // Entity permissions
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <summary>
        /// Nadanie podmiotom uprawnień do pracy w KSeF
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsEntityRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionEntityAsync(GrantPermissionsEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rozpoczyna asynchroniczną operację nadawania uprawnień podmiotowych.
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsAuthorizationRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsAuthorizationPermissionAsync(GrantPermissionsAuthorizationRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nadanie uprawnień w sposób pośredni
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsIndirectEntityRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionIndirectEntityAsync( GrantPermissionsIndirectEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nadanie uprawnień administratora podmiotu podrzędnego
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsSubunitRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionSubUnitAsync( GrantPermissionsSubunitRequest requestPayload,  string accessToken, CancellationToken cancellationToken = default);


        /// <summary>
        /// Nadanie uprawnień administratora podmiotu unijnego
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsEuEntityRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionEUEntityAsync( GrantPermissionsEuEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);


        /// <summary>
        /// Nadanie uprawnień administratora podmiotu unijnego
        /// </summary>
        /// <param name="requestPayload"><see cref="GrantPermissionsEuEntityRepresentativeRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="OperationResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync( GrantPermissionsEuEntityRepresentativeRequest requestPayload,  string accessToken,  CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie danych o limitach certyfikatów.
        /// Zwraca informacje o limitach certyfikatów oraz informacje czy użytkownik może zawnioskować o certyfikat.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="CertificateLimitResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateLimitResponse> GetCertificateLimitsAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca dane wymagane do przygotowania wniosku certyfikacyjnego.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="CertificateEnrollmentsInfoResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(string accessToken, CancellationToken cancellationToken = default);
  
        /// <summary>
        /// Przyjmuje wniosek certyfikacyjny i rozpoczyna jego przetwarzanie.
        /// </summary>
        /// <param name="requestPayload"><see cref="SendCertificateEnrollmentRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="CertificateEnrollmentResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(SendCertificateEnrollmentRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca informacje o statusie wniosku certyfikacyjnego.
        /// </summary>
        /// <param name="certificateRequestReferenceNumber">Numer referencyjny wniosku certyfikacyjnego.</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="CertificateEnrollmentStatusResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string certificateRequestReferenceNumber, string accessToken, CancellationToken cancellationToken = default);
   
        /// <summary>
        /// Zwraca certyfikaty o podanych numerach seryjnych w formacie DER zakodowanym w Base64.
        /// </summary>
        /// <param name="requestPayload"><see cref="CertificateListRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="CertificateListResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unieważnia certyfikat o podanym numerze seryjnym.
        /// </summary>
        /// <param name="requestPayload"><see cref="CertificateRevokeRequest"/></param>
        /// <param name="serialNumber">Numer seryjny certyfikatu</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task RevokeCertificateAsync(CertificateRevokeRequest requestPayload, string serialNumber, string accessToken, CancellationToken cancellationToken = default);
   
        /// <summary>
        /// Zwraca listę certyfikatów spełniających podane kryteria wyszukiwania. W przypadku braku podania kryteriów wyszukiwania zwrócona zostanie nieprzefiltrowana lista.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="requestPayload"><see cref="CertificateMetadataListRequest"/></param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="pageOffset">Index strony wyników (domyślnie 0)</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="CertificateMetadataListResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<CertificateMetadataListResponse> GetCertificateMetadataListAsync(string accessToken, CertificateMetadataListRequest requestPayload = null, int? pageSize = null, int? pageOffset = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Generuje nowy token KSeF.
        /// </summary>
        /// <param name="requestPayload"><see cref="KsefTokenRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="KsefTokenResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<KsefTokenResponse> GenerateKsefTokenAsync(KsefTokenRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy wygenerowanych tokenów.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="status">Status tokenów do zwrócenia (można podać wielokrotnie).</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="QueryKsefTokensResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<QueryKsefTokensResponse> QueryKsefTokensAsync(
        string accessToken,
        ICollection<AuthenticationKsefTokenStatus> statuses = null,
        string authorIdentifier = null,
        Models.Token.TokenContextIdentifierType? authorIdentifierType = null,
        string description = null,
        string continuationToken = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusu tokena
        /// </summary>
        /// <param name="tokenReferenceNumber">Numer referencyjny tokena.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationKsefToken"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<AuthenticationKsefToken> GetKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unieważnienie tokena.
        /// </summary>
        /// <param name="tokenReferenceNumber">Numer referencyjny tokena.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation toke.</param>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task RevokeKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Wysłanie części paczki faktur.
        /// </summary>
        /// <param name="openBatchSessionResponse"><see cref="OpenBatchSessionResponse"/></param>
        /// <param name="parts">Kolekcja trzymająca informacje o partach</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="AggregateException"></exception>
        Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Wysłanie części paczki faktur z wykorzystaniem strumienia.
        /// </summary>
        /// <param name="openBatchSessionResponse"><see cref="OpenBatchSessionResponse"/></param>
        /// <param name="parts">Kolekcja trzymająca informacje o partach</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="AggregateException"></exception>
        Task SendBatchPartsWithStreamAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartStreamSendingInfo> parts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy dostawców usług Peppol.
        /// </summary>
        /// <param name="accessToken">Bearer access token.</param>
        /// <param name="pageOffset">Numer strony wyników (opcjonalny).</param>
        /// <param name="pageSize">Rozmiar strony wyników (opcjonalny).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="QueryPeppolProvidersResponse"/></returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<QueryPeppolProvidersResponse> QueryPeppolProvidersAsync(
            string accessToken,
            int? pageOffset = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inicjuje eksport paczki faktur zgodnie z podanymi filtrami.
        /// </summary>
        /// <param name="requestPayload">Żądanie eksportu faktur (filtry + szyfrowanie).</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="OperationResponse"/> zawierający numer referencyjny operacji.</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OperationResponse> ExportInvoicesAsync(
            InvoiceExportRequest requestPayload,
            string accessToken,
            CancellationToken cancellationToken = default,
            bool includeMetadata = true);

        /// <summary>
        /// Pobiera status operacji eksportu paczki faktur.
        /// </summary>
        /// <param name="referenceNumber">Numer referencyjny operacji eksportu.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="InvoiceExportStatusResponse"/> zawierający status oraz paczkę faktur (jeśli dostępna).</returns>
        /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<InvoiceExportStatusResponse> GetInvoiceExportStatusAsync(
            string referenceNumber,
            string accessToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobiera UPO z adresu Uri.
        /// </summary>
        /// <param name="url">Url do pobrania UPO.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<string> GetUpoAsync(Uri uri, CancellationToken cancellationToken = default);
    }
}