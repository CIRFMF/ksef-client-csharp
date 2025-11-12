using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Klient do operacji uwierzytelniania i autoryzacji.
    /// </summary>
    public interface IAuthorizationClient
    {
        /// <summary>
        /// Inicjalizacja mechanizmu uwierzytelnienia i autoryzacji.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationChallengeResponse"/></returns>
        Task<AuthenticationChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rozpoczyna operację uwierzytelniania za pomocą podpisanego dokumentu XML (XAdES).
        /// </summary>
        /// <param name="signedXML">Podpisany XML.</param>
        /// <param name="verifyCertificateChain">Czy sprawdzić łańcuch certyfikatów.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="SignatureResponse"/></returns>
        Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rozpoczyna operację uwierzytelniania z wykorzystaniem wcześniej wygenerowanego tokena KSeF.
        /// </summary>
        /// <param name="requestPayload"><see cref="AuthenticationKsefTokenRequest"/></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="SignatureResponse"/></returns>
        Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthenticationKsefTokenRequest requestPayload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sprawdza bieżący status operacji uwierzytelniania dla podanego tokena.
        /// </summary>
        /// <param name="authOperationReferenceNumber">Numer referencyjny operacji.</param>
        /// <param name="authenticationToken">Tymczasowy token otrzymany po inicjalizacji.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthStatus"/></returns>
        Task<AuthStatus> GetAuthStatusAsync(string authOperationReferenceNumber, string authenticationToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pobranie tokenów dostępowych po udanym uwierzytelnieniu.
        /// </summary>
        /// <param name="authenticationToken">Tymczasowy token uwierzytelnienia.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="AuthenticationOperationStatusResponse"/></returns>
        Task<AuthenticationOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Odświeża access token wykorzystując refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="RefreshTokenResponse"/></returns>
        Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
