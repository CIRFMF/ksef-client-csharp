using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Operacje sekcji „Dane testowe” + pomocnicze „Query Grants” (weryfikacja efektów).
    /// </summary>
    public interface ITestDataClient
    {
        /// <summary>
        /// POST /api/v2/testdata/subject — utwórz podmiot testowy.
        /// </summary>
        Task<Status> CreateSubjectAsync(SubjectCreateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/subject/remove — usuń podmiot testowy.
        /// </summary>
        Task<Status> RemoveSubjectAsync(SubjectRemoveRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/person — utwórz osobę testową.
        /// </summary>
        Task<Status> CreatePersonAsync(PersonCreateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/person/remove — usuń osobę testową.
        /// </summary>
        Task<Status> RemovePersonAsync(PersonRemoveRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/permissions — nadaj uprawnienia testowe.
        /// </summary>
        Task<Status> GrantPermissionsAsync(TestDataPermissionsGrantRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/permissions/revoke — cofnij uprawnienia testowe.
        /// </summary>
        Task<Status> RevokePermissionsAsync(TestDataPermissionsRevokeRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/attachment — włącz załączniki (test).
        /// </summary>
        Task<Status> EnableAttachmentAsync(AttachmentPermissionGrantRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/attachment/revoke — wyłącz załączniki (test).
        /// </summary>
        Task<Status> DisableAttachmentAsync(AttachmentPermissionRevokeRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/limits/context/session — zmiana limitów sesji dla bieżącego kontekstu (tylko na środowiskach testowych).
        /// </summary>
        Task<Status> ChangeSessionLimitsInCurrentContextAsync(ChangeSessionLimitsInCurrentContextRequest request, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE /api/v2/testdata/limits/context/session — przywrócenie domyślnych limitów sesji dla bieżącego kontekstu (tylko na środowiskach testowych).
        /// </summary>
        Task<Status> RestoreDefaultSessionLimitsInCurrentContextAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/limits/context/certificates — zmiana limitów certyfikatów dla bieżącego podmiotu (tylko na środowiskach testowych).
        /// </summary>
        Task<Status> ChangeCertificatesLimitInCurrentSubjectAsync(ChangeCertificatesLimitInCurrentSubjectRequest request, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE /api/v2/testdata/limits/context/certificates — przywrócenie domyślnych limitów certyfikatów dla bieżącego podmiotu.
        /// </summary>
        Task<Status> RestoreDefaultCertificatesLimitInCurrentSubjectAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/rate-limits — zmienia wartości aktualnie obowiązujących limitów żądań przesyłanych do API dla bieżącego kontekstu. Tylko na środowisku testowym.
        /// </summary>
        Task<Status> SetRateLimitsAsync(EffectiveApiRateLimitsRequest requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE /api/v2/testdata/rate-limits — przywraca wartości aktualnie obowiązujących limitów żądań przesyłanych do API dla bieżącego kontekstu do wartości domyślnych. Tylko na środowiskach testowych.
        /// </summary>
        Task<Status> RestoreRateLimitsAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/rate-limits/production - ustawia w bieżącym kontekście wartości limitów API zgodne z profilem produkcyjnym.
        /// </summary>
        Task<Status> RestoreProductionRateLimitsAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/context/block — Blokuje możliwość uwierzytelniania dla bieżącego kontekstu. Tylko na środowiskach testowych.
        /// </summary>
        Task<Status> BlockContextAsync(ContextIdentifier requestPayload, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /api/v2/testdata/context/unblock — Odblokowuje możliwość uwierzytelniania dla bieżącego kontekstu. Tylko na środowiskach testowych.
        /// </summary>
        Task<Status> UnblockContextAsync(ContextIdentifier requestPayload, string accessToken, CancellationToken cancellationToken = default);
    }
}
