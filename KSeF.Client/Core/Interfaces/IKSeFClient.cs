
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.ProxyEntity;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeFClient.Core.Models;

namespace KSeFClient;

public interface IKSeFClient
{
    /// <summary>
    /// Inicjalizacja mechanizmu uwierzytelnienia i autoryzacji
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AuthChallengeResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 bad request)</exception>
    Task<AuthChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozpoczyna operację uwierzytelniania za pomocą dokumentu XML podpisanego podpisem elektroniczny XAdES.
    /// </summary>
    /// <remarks>
    /// Rozpoczyna proces uwierzytelnienia na podstawie podpisanego XML-a.
    /// </remarks>
    /// <param name="signedXML">Podpisany XML z żądaniem uwierzytelnienia.</param>
    /// <param name="verifyCertificateChain">Flaga określająca, czy sprawdzić łańcuch certyfikatów. (Domyślnie false)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="SignatureResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 bad request)</exception>
    Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozpoczyna operację uwierzytelniania z wykorzystaniem wcześniej wygenerowanego tokena KSeF.
    /// </summary>
    /// <param name="requestPayload"><see cref="AuthKsefTokenRequest"/></param>
    /// <returns><see cref="SignatureResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthKsefTokenRequest requestPayload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza bieżący status operacji uwierzytelniania dla podanego tokena.
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny otrzymany w wyniku inicjalizacji uwierzytelnienia.</param>
    /// <param name="authenticationToken">Tymczasowy token otrzymany w wyniku inicjalizacji uwierzytelnienia.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="BasicStatusInfo"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<AuthStatus> GetAuthStatusAsync(string referenceNumberstring, string authenticationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie tokena dostępowego.
    /// </summary>
    /// <remarks>
    /// Zwraca accessToken i refreshToken
    /// </remarks>
    /// <param name="authenticationToken">Tymczasowy token otrzymany w wyniku inicjalizacji uwierzytelnienia.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AuthOperationStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<AuthOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Odświeżanie tokenu dostępu
    /// </summary>
    /// <remarks>
    /// Zwraca odświezony access token
    /// </remarks>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="RefreshTokenResponse"/></returns>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    /// <exception cref="ApiException">W przypadku podania accessToken zamiast refreshToken. (403 Forbidden)</exception>
    Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);


    /// <summary>
    /// Unieważnia aktualny token autoryzacyjny przekazany w nagłówku żądania. Po unieważnieniu token nie może być używany do żadnych operacji.
    /// </summary>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task RevokeAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca informacje o kluczach publicznych używanych do szyfrowania danych przesyłanych do systemu KSeF.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    Task<ICollection<PemCertificateInfo>> GetPublicCertificates(CancellationToken cancellationToken);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
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
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="SendInvoiceResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string referenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamknięcie sesji interaktywnej
    /// </summary>
    /// <remarks>
    /// Zamyka sesję interaktywną i rozpoczyna generowanie zbiorczego UPO.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task CloseOnlineSessionAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);

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
    /// <param name="referenceNumber">Numer referencyjny sesji wsadowej.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task CloseBatchSessionAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie statusu sesji
    /// </summary>
    /// <remarks>
    /// Zwraca informacje o statusie sesji.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token.</param>"
    /// <returns><see cref="SessionStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<SessionStatusResponse> GetSessionStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie statusów dokumentów sesji
    /// </summary>
    /// <remarks>
    /// Zwraca listę dokumentów przesłanych w sesji wraz z ich statusami, oraz informacje na temat ilości poprawnie i niepoprawnie przetworzonych dokumentów.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="pageOffset">Numer strony wyników.</param>
    /// <param name="pageSize">Rozmiar strony wyników.</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns><see cref="SessionInvoicesResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string referenceNumber, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie statusu faktury z sesji
    /// </summary>
    /// <remarks>Zwraca fakturę przesłaną w sesji wraz ze statusem.</remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji.</param>
    /// <param name="invoiceReferenceNumber">Numer referencyjny faktury.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns><see cref="SessionInvoice"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<SessionInvoice> GetSessionInvoiceAsync(string referenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie niepoprawnie przetworzonych dokumentów sesji
    /// </summary>
    /// <remarks>
    /// Zwraca listę niepoprawnie przetworzonych dokumentów przesłanych w sesji wraz z ich statusami.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="pageSize">Rozmiar strony wyników.</param>
    /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns><see cref="SessionInvoicesResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<SessionInvoicesResponse> GetSessionFailedInvoicesAsync(string referenceNumber, string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default);

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Pobranie UPO faktuy z sesji na podstawie numeru KSeF
    /// </summary>
    /// <remarks>
    /// Zwraca UPO faktuy przesłanej w sesji na podstawie jego numeru KSeF.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="ksefNumber">Numer KSeF faktuy</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns>UPO w formie XML</returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<string> GetSessionInvoiceUpoByKsefNumberAsync(string referenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie UPO faktury z sesji na podstawie numeru referencyjnego faktury.
    /// </summary>
    /// <remarks>
    /// Zwraca UPO faktury przesłanego w sesji na podstawie jego numeru KSeF.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="invoiceReferenceNumber">Numer referencyjny faktury</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns>UPO w formie XML</returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<string> GetSessionInvoiceUpoByReferenceNumberAsync(string referenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie UPO dla sesji
    /// </summary>
    /// <remarks>
    /// Zwraca XML zawierający zbiorcze UPO dla sesji.
    /// </remarks>
    /// <param name="referenceNumber">Numer referencyjny sesji</param>
    /// <param name="upoReferenceNumber">Numer referencyjny UPO</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns>Zbiorcze UPO w formie XML</returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<string> GetSessionUpoAsync(string referenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie faktury po numerze KSeF
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF faktury</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns>Faktura w formie XML.</returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<string> GetInvoiceAsync(string ksefReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie faktury na podstawie numeru KSeF oraz danych faktury.
    /// </summary>
    /// <param name="requestPayload"><see cref="InvoiceRequest"/>Dane faktury.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Faktura w formie XML.</returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<string> DownloadInvoiceAsync(InvoiceRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca listę metadanych faktur spełniające podane kryteria wyszukiwania.
    /// </summary>
    /// <param name="requestPayload"><see cref="QueryInvoiceRequest"/>zestaw filtrów</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="pageOffset">Numer strony wyników.</param>
    /// <param name="pageSize">Rozmiar strony wyników.</param>
    /// <param name="cancellationToken">Cancellation token./param>
    /// <returns><see cref="PagedInvoiceResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<PagedInvoiceResponse> QueryInvoicesAsync(QueryInvoiceRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozpoczyna asynchroniczny proces wyszukiwania faktur w systemie KSeF na podstawie przekazanych filtrów
    /// </summary>
    /// <param name="requestPayload"><see cref="AsyncQueryInvoiceRequest"/> zestaw filtrów i informacja o szyfrowaniu</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="OperationStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationStatusResponse> AsyncQueryInvoicesAsync(AsyncQueryInvoiceRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera status wcześniej zainicjalizowanego zapytania asynchronicznego na podstawie identyfikatora operacji.
    /// Umożliwia śledzenie postępu przetwarzania zapytania oraz pobranie gotowych paczek z wynikami, jeśli są już dostępne.
    /// </summary>
    /// <param name="operationReferenceNumber">Numer referencyjny operacji.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AsyncQueryInvoiceStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<AsyncQueryInvoiceStatusResponse> GetAsyncQueryInvoicesStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie statusu operacji - uprawnienia
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny operacji.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AsyncQueryInvoiceStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);



    /// <summary>
    /// Rozpoczyna asynchroniczną operację odbierania uprawnienia o podanym identyfikatorze.
    /// </summary>
    /// <param name="permissionId">Id uprawnienia.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="cancellationToken">Canccelation token.</param>
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
    /// <param name="cancellationToken">Canccelation token.</param>
    /// <returns><see cref="OperationResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationResponse> RevokeAuthorizationsPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobranie listy uprawnień do pracy w KSeF nadanych osobom fizycznym lub podmiotom.
    /// </summary>
    /// <param name="requestPayload"><see cref="PersonPermissionsQueryRequest"/></param>
    /// <param name="accessToken">Acces token</param>
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
    /// <param name="accessToken">Acces token</param>
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
    /// <param name="accessToken">Acces token</param>
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
    /// <param name="accessToken">Acces token</param>
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
    /// <param name="accessToken">Acces token</param>
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
    /// <param name="accessToken">Acces token</param>
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
    /// Nadanie podmiotom uprawnień o charakterze upoważnień
    /// </summary>
    /// <param name="requestPayload"><see cref="GrantPermissionsProxyEntityRequest"/></param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="OperationResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationResponse> GrantsPermissionProxyEntityAsync(GrantPermissionsProxyEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

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
    /// <param name="requestPayload"><see cref="GrantPermissionsSubUnitRequest"/></param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="OperationResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationResponse> GrantsPermissionSubUnitAsync( GrantPermissionsSubUnitRequest requestPayload,  string accessToken, CancellationToken cancellationToken = default);


    /// <summary>
    /// Nadanie uprawnień administratora podmiotu unijnego
    /// </summary>
    /// <param name="requestPayload"><see cref="GrantPermissionsRequest"/></param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="OperationResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationResponse> GrantsPermissionEUEntityAsync( GrantPermissionsRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);


    /// <summary>
    /// Nadanie uprawnień administratora podmiotu unijnego
    /// </summary>
    /// <param name="requestPayload"><see cref="GrantPermissionsEUEntitRepresentativeRequest"/></param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="OperationResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync( GrantPermissionsEUEntitRepresentativeRequest requestPayload,  string accessToken,  CancellationToken cancellationToken = default);

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
    /// <param name="referenceNumber">Numer refrencyjny wniosku certyfikacyjnego.</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="CertificateEnrollmentStatusResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);
   
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
    /// Gneruje nowy token KSeF.
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
    /// <param name="statuses">Statusy</param>
    /// <param name="continuationToken">Contnuation token.</param>
    /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="QueryKsefTokensResponse"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<QueryKsefTokensResponse> QueryKsefTokensAsync(string accessToken, ICollection<AuthenticationKsefTokenStatus> statuses = null, string continuationToken = null, int? pageSize = null, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Pobranie statusu tokena
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny tokena.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AuthenticationKsefToken"/></returns>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task<AuthenticationKsefToken> GetKsefTokenAsync(string refrenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unieważnienie tokena.
    /// </summary>
    /// <param name="referenceNumber">Numer referencyjny tokena.</param>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation toke.</param>
    /// <exception cref="ApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
    /// <exception cref="ApiException">Brak autoryzacji. (401 Unauthorized)</exception>
    Task RevokeKsefTokenAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wysłanie części paczki faktur
    /// </summary>
    /// <param name="openBatchSessionResponse"><see cref="OpenBatchSessionResponse"/></param>
    /// <param name="parts">Kolekcja trzymająca informacje o partach</param>
    /// <param name="cancellationToken">Cancellaton token</param>
    /// <exception cref="AggregateException"></exception>
    Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default);

}