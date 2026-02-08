using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Http.Helpers;
using System.Text;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class CertificateClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), ICertificateClient
{
    /// <inheritdoc />
    public Task<CertificateLimitResponse> GetCertificateLimitsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        return ExecuteAsync<CertificateLimitResponse>(Routes.Certificates.Limits, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        return ExecuteAsync<CertificateEnrollmentsInfoResponse>(Routes.Certificates.EnrollmentData, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(SendCertificateEnrollmentRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        return ExecuteAsync<CertificateEnrollmentResponse, SendCertificateEnrollmentRequest>(Routes.Certificates.Enrollments, requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string certificateRequestReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(certificateRequestReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        string endpoint = Routes.Certificates.EnrollmentStatus(Uri.EscapeDataString(certificateRequestReferenceNumber));
        return ExecuteAsync<CertificateEnrollmentStatusResponse>(endpoint, HttpMethod.Get, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        return ExecuteAsync<CertificateListResponse, CertificateListRequest>(Routes.Certificates.Retrieve, requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeCertificateAsync(CertificateRevokeRequest requestPayload, string serialNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(serialNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);
        string endpoint = Routes.Certificates.Revoke(Uri.EscapeDataString(serialNumber));

        return ExecuteAsync(endpoint, requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CertificateMetadataListResponse> GetCertificateMetadataListAsync(string accessToken, CertificateMetadataListRequest requestPayload = null, int? pageSize = null, int? pageOffset = null, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new(Routes.Certificates.Query);
        PaginationHelper.AppendPagination(pageOffset, pageSize, urlBuilder);
        string endpoint = urlBuilder.ToString();

        return ExecuteAsync<CertificateMetadataListResponse, CertificateMetadataListRequest>(endpoint, requestPayload ?? new CertificateMetadataListRequest(), accessToken, cancellationToken);
    }
}
