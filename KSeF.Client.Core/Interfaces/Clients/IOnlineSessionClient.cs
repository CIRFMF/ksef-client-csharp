using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    public interface IOnlineSessionClient
    {
        /// <summary>
        /// Otwarcie sesji interaktywnej
        /// </summary>
        /// <remarks>
        /// Inicjalizacja wysyłki interaktywnej faktur.
        /// </remarks>
        /// <param name="accessToken">Access token.</param>
        /// <param name="requestPayload"><see cref="OpenOnlineSessionRequest"/></param>
        /// <param name="feature">
        /// Opcjonalna wartość nagłówka <c>X-KSeF-Feature</c>.
        /// Dostępna wartość: <see cref="KsefFeatures.SubjectIdentifierValidation"/> - włącza walidację numerów NIP
        /// oraz identyfikatorów wewnętrznych podmiotów wskazanych na fakturze.
        /// Funkcjonalność dostępna jedynie na środowisku testowym.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="OpenOnlineSessionResponse"/></returns>
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, string feature = null, CancellationToken cancellationToken = default);

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
        /// <exception cref="KsefApiException">Nieprawidłowe żądanie. (400 Bad request)</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
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
        /// <exception cref="KsefApiException">A server side error occurred.</exception>
        /// <exception cref="KsefApiException">Brak autoryzacji. (401 Unauthorized)</exception>
        Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default);
    }
}
