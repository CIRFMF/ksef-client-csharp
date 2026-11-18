using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Klient do zarządzania tokenami KSeF.
    /// </summary>
    public interface IKsefTokenClient
    {
        /// <summary>
        /// Generuje nowy token KSeF.
        /// </summary>
        /// <param name="requestPayload"><see cref="KsefTokenRequest"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="KsefTokenResponse"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        [Obsolete("Operacja POST /tokens jest przestarzała i zostanie wyłączona 30.09.2027. Użyj GenerateKsefTokenRev2Async.")]
        Task<KsefTokenResponse> GenerateKsefTokenAsync(KsefTokenRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy wygenerowanych tokenów.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="statuses">Statusy tokenów do zwrócenia (można podać wielokrotnie).</param>
        /// <param name="authorIdentifier">Identyfikator uwierzytelnienia</param>
        /// <param name="authorIdentifierType">Typ identyfikatora</param>
        /// <param name="description">Opis</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="pageSize">Ilość elementów na stronie (domyślnie 10)</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="QueryKsefTokensResponse"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        [Obsolete("Operacja GET /tokens jest przestarzała i zostanie wyłączona 30.09.2027. Użyj QueryKsefTokensRev2Async.")]
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
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        [Obsolete("Operacja GET /tokens/{referenceNumber} jest przestarzała i zostanie wyłączona 30.09.2027. Użyj GetKsefTokenRev2Async.")]
        Task<AuthenticationKsefToken> GetKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unieważnienie tokena.
        /// </summary>
        /// <param name="tokenReferenceNumber">Numer referencyjny tokena.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation toke.</param>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task RevokeKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zatwierdzenie tokena wygenerowanego po uwierzytelnieniu tokenem KSeF (POST /tokens/{referenceNumber}/approve).
        /// Do czasu zatwierdzenia token nie może być użyty do uwierzytelnienia.
        /// </summary>
        /// <param name="tokenReferenceNumber">Numer referencyjny tokena.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task ApproveKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generuje nowy token KSeF o określonym okresie ważności (POST /tokens/rev2).
        /// Token może być generowany tylko w kontekście NIP lub identyfikatora wewnętrznego.
        /// </summary>
        /// <param name="requestPayload"><see cref="KsefTokenRequestRev2"/></param>
        /// <param name="accessToken">Access token</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="KsefTokenResponseRev2"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<KsefTokenResponseRev2> GenerateKsefTokenRev2Async(KsefTokenRequestRev2 requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie listy wygenerowanych tokenów w kontrakcie Rev2 (GET /tokens/rev2).
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="statuses">Statusy tokenów do zwrócenia (można podać wielokrotnie).</param>
        /// <param name="authorIdentifier">Identyfikator twórcy tokena (min. 3 znaki).</param>
        /// <param name="authorIdentifierType">Typ identyfikatora twórcy tokena.</param>
        /// <param name="description">Opis tokena (min. 3 znaki).</param>
        /// <param name="validFrom">Filtruje tokeny, których <c>validFrom</c> przypada w tym samym momencie lub później niż wskazana data.</param>
        /// <param name="validTo">Filtruje tokeny, których <c>validTo</c> przypada w tym samym momencie lub wcześniej niż wskazana data.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="pageSize">Rozmiar strony (10-100, domyślnie 10).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="QueryKsefTokensResponseRev2"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<QueryKsefTokensResponseRev2> QueryKsefTokensRev2Async(
            string accessToken,
            ICollection<KsefTokenStatusFilterRev2> statuses = null,
            string authorIdentifier = null,
            Models.Token.TokenContextIdentifierType? authorIdentifierType = null,
            string description = null,
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validTo = null,
            string continuationToken = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie statusu tokena w kontrakcie Rev2 (GET /tokens/rev2/{referenceNumber}).
        /// </summary>
        /// <param name="tokenReferenceNumber">Numer referencyjny tokena.</param>
        /// <param name="accessToken">Access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationKsefTokenRev2"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<AuthenticationKsefTokenRev2> GetKsefTokenRev2Async(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default);
    }
}
