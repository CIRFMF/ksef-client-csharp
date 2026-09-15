namespace KSeF.DemoWebApp.Controllers;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.TestData;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// UWAGA: wszystkie poniższe endpointy działają wyłącznie na środowisku testowym KSeF
/// Na produkcji zwracają błąd. Służą do przygotowania stanu przed testami:
/// tworzenia podmiotów/osób, nadawania uprawnień, modyfikowania limitów, blokowania kontekstu itp.
/// Nie należy ich wywoływać w normalnym przepływie integracji.
/// </summary>
[ApiController]
[Route("testdata")]
public class TestDataController(ITestDataClient testDataClient) : ControllerBase
{
    /// <summary>Utworzenie podmiotu testowego.</summary>
    [HttpPost("subject")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> CreateSubject([FromBody] SubjectCreateRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.CreateSubjectAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Usunięcie podmiotu testowego.</summary>
    [HttpPost("subject/remove")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RemoveSubject([FromBody] SubjectRemoveRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.RemoveSubjectAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Utworzenie osoby testowej.</summary>
    [HttpPost("person")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> CreatePerson([FromBody] PersonCreateRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.CreatePersonAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Usunięcie osoby testowej.</summary>
    [HttpPost("person/remove")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RemovePerson([FromBody] PersonRemoveRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.RemovePersonAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Nadanie uprawnień testowych.</summary>
    [HttpPost("permissions")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> GrantPermissions([FromBody] TestDataPermissionsGrantRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.GrantPermissionsAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Cofnięcie uprawnień testowych.</summary>
    [HttpPost("permissions/revoke")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RevokePermissions([FromBody] TestDataPermissionsRevokeRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.RevokePermissionsAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Włączenie obsługi faktur z załącznikiem dla podmiotu (test).</summary>
    [HttpPost("attachment")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> EnableAttachment([FromBody] AttachmentPermissionGrantRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.EnableAttachmentAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Wyłączenie obsługi faktur z załącznikiem dla podmiotu (test).</summary>
    [HttpPost("attachment/revoke")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> DisableAttachment([FromBody] AttachmentPermissionRevokeRequest request, CancellationToken cancellationToken)
        => Ok(await testDataClient.DisableAttachmentAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Zmiana limitów sesji dla bieżącego kontekstu.</summary>
    [HttpPost("limits/context/session")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> ChangeSessionLimits(
        string accessToken,
        [FromBody] ChangeSessionLimitsInCurrentContextRequest request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.ChangeSessionLimitsInCurrentContextAsync(request, accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Przywrócenie domyślnych limitów sesji dla bieżącego kontekstu.</summary>
    [HttpDelete("limits/context/session")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RestoreSessionLimits(
        string accessToken,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.RestoreDefaultSessionLimitsInCurrentContextAsync(accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Zmiana limitu certyfikatów dla bieżącego podmiotu.</summary>
    [HttpPost("limits/subject/certificate")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> ChangeCertificatesLimit(
        string accessToken,
        [FromBody] ChangeCertificatesLimitInCurrentSubjectRequest request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.ChangeCertificatesLimitInCurrentSubjectAsync(request, accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Przywrócenie domyślnego limitu certyfikatów dla bieżącego podmiotu.</summary>
    [HttpDelete("limits/subject/certificate")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RestoreCertificatesLimit(
        string accessToken,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.RestoreDefaultCertificatesLimitInCurrentSubjectAsync(accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Ustawienie wartości limitów żądań (rate limits) dla bieżącego kontekstu.</summary>
    [HttpPost("rate-limits")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> SetRateLimits(
        string accessToken,
        [FromBody] EffectiveApiRateLimitsRequest request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.SetRateLimitsAsync(request, accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Przywrócenie domyślnych wartości limitów żądań dla bieżącego kontekstu.</summary>
    [HttpDelete("rate-limits")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RestoreRateLimits(
        string accessToken,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.RestoreRateLimitsAsync(accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Ustawienie limitów API zgodnych z profilem produkcyjnym w bieżącym kontekście.</summary>
    [HttpPost("rate-limits/production")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> RestoreProductionRateLimits(
        string accessToken,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.RestoreProductionRateLimitsAsync(accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Zablokowanie możliwości uwierzytelniania dla bieżącego kontekstu.</summary>
    [HttpPost("context/block")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> BlockContext(
        string accessToken,
        [FromBody] ContextIdentifier request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.BlockContextAsync(request, accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Odblokowanie możliwości uwierzytelniania dla bieżącego kontekstu.</summary>
    [HttpPost("context/unblock")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> UnblockContext(
        string accessToken,
        [FromBody] ContextIdentifier request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.UnblockContextAsync(request, accessToken, cancellationToken).ConfigureAwait(false));

    /// <summary>Aktualizacja danych wskazanego certyfikatu testowego (np. daty ważności).</summary>
    [HttpPut("certificates/{serialNumber}")]
    [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
    public async Task<ActionResult<Status>> UpdateCertificate(
        string accessToken,
        string serialNumber,
        [FromBody] TestDataUpdateCertificateRequest request,
        CancellationToken cancellationToken)
        => Ok(await testDataClient.UpdateCertificateAsync(serialNumber, request, accessToken, cancellationToken).ConfigureAwait(false));
}
