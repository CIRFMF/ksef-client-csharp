using KSeF.Client.Core.Interfaces;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Certificates;
using KSeFClient.Api.Builders.Certificates;

namespace WebApplication.Controllers;

[Route("[controller]")]
[ApiController]
public class CertificateController : ControllerBase
{
    private readonly IKSeFClient kSeFClient;
    public CertificateController(IKSeFClient kSeFClient)
    {
        this.kSeFClient = kSeFClient;
    }

    [HttpGet("limits")]
    public async Task<ActionResult<CertificateLimitResponse>> GetLimitsAsync(string accessToken, CancellationToken cancellationToken)
    {
        return await kSeFClient.GetCertificateLimitsAsync(accessToken, cancellationToken)
             .ConfigureAwait(false);
    }

    [HttpGet("enrollment-info")]
    public async Task<ActionResult<CertificateEnrollmentsInfoResponse>> GetEnrollmentsInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        return await kSeFClient.GetCertificateEnrollmentDataAsync(accessToken, cancellationToken)
             .ConfigureAwait(false);
    }

    [HttpPost("send-enrollment")]
    public async Task<ActionResult<CertificateEnrollmentResponse>> SendEnrollmentAsync([FromBody] CertificateEnrollmentsInfoResponse requestPayload, [FromServices] ICryptographyService cryptographyService, string accessToken, CancellationToken cancellationToken)
    {
        (var csrBase64encoded, var privateKeyBase64Encoded) = cryptographyService.GenerateCsr(requestPayload);
        var enrollmentRequest = SendCertificateEnrollmentRequestBuilder.Create()
            .WithCertificateName("Testowy certyfikat")
            .WithCsr(csrBase64encoded)
            .WithValidFrom(DateTimeOffset.UtcNow.AddDays(1)) // Certyfikat będzie ważny od jutra
            .Build();

        return await kSeFClient.SendCertificateEnrollmentAsync(enrollmentRequest, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpGet("enrollment-status/{referenceNumber}")]
    public async Task<ActionResult<CertificateEnrollmentStatusResponse>> GetEnrollmentStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        return await kSeFClient.GetCertificateEnrollmentStatusAsync(referenceNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpPost("retrieve")]
    public async Task<ActionResult<CertificateListResponse>> GetCertificateAsync(List<string> serialNumbers, string accessToken, CancellationToken cancellationToken)
    {
        return await kSeFClient.GetCertificateListAsync(new CertificateListRequest { CertificateSerialNumbers = serialNumbers }, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeCertificateAsync(string serialNumber, string accessToken, CancellationToken cancellationToken)
    {
        var request = RevokeCertificateRequestBuilder.Create()
            .WithRevocationReason(CertificateRevocationReason.KeyCompromise) // optional
            .Build();
        await kSeFClient.RevokeCertificateAsync(request, serialNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);
        return Ok();
    }

    [HttpGet("certificate-list")]
    public async Task<ActionResult<CertificateMetadataListResponse>> GetCertificateMetadataListAsync(string accessToken, CancellationToken cancellationToken)
    {
        return await kSeFClient.GetCertificateMetadataListAsync(accessToken, null, null, null, cancellationToken)
          .ConfigureAwait(false);
    }
}
