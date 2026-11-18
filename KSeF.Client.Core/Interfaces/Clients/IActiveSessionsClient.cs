using System;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.Sessions.ActiveSessions.Rev2;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Klient do operacji na aktywnych sesjach uwierzytelniania.
    /// </summary>
    public interface IActiveSessionsClient
    {
        /// <summary>
        /// Pobranie listy aktywnych sesji uwierzytelnienia.
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Rozmiar strony wyników.</param>
        /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationListResponse"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        [Obsolete("Operacja GET /auth/sessions jest przestarzała i zostanie wyłączona 15.06.2027. Użyj GetActiveSessionsRev2Async.")]
        Task<AuthenticationListResponse> GetActiveSessions(string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy aktywnych sesji uwierzytelnienia (rewizja Rev2, <c>GET /auth/sessions/rev2</c>).
        /// </summary>
        /// <param name="accessToken">Access token</param>
        /// <param name="pageSize">Rozmiar strony wyników (10–100).</param>
        /// <param name="continuationToken">Token kontynuacji, jeśli jest dostępny.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationListResponseRev2"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<AuthenticationListResponseRev2> GetActiveSessionsRev2Async(string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unieważnia sesję powiązaną z tokenem użytym do wywołania tej operacji.
        /// Unieważnienie sesji sprawia, że powiązany z nią refresh token przestaje działać i nie można już za jego pomocą uzyskać kolejnych access tokenów.
        /// Aktywne access tokeny działają do czasu upłynięcia ich terminu ważności.
        /// </summary>
        /// <param name="token">Access token lub Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task RevokeCurrentSessionAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unieważnia sesję o podanym numerze referencyjnym.
        /// Unieważnienie sesji sprawia, że powiązany z nią refresh token przestaje działać i nie można już za jego pomocą uzyskać kolejnych access tokenów.
        /// Aktywne access tokeny działają do czasu upłynięcia ich terminu ważności.
        /// </summary>
        /// <param name="sessionReferenceNumber">Numer referencyjny sesji.</param>
        /// <param name="accessToken">Access token lub Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task RevokeSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);
    }
}
