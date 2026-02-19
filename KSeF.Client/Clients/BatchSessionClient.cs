using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Helpers;
using KSeF.Client.Core.Interfaces.Clients;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class BatchSessionClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IBatchSessionClient
{
    /// <inheritdoc />
    public Task<OpenBatchSessionResponse> OpenBatchSessionAsync(OpenBatchSessionRequest requestPayload, string accessToken, string upoVersion, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        return ExecuteAsync<OpenBatchSessionResponse, OpenBatchSessionRequest>(
            Routes.Sessions.Batch.Open,
            requestPayload,
            accessToken,
			!string.IsNullOrEmpty(upoVersion) ?
            new Dictionary<string, string> 
                { { "X-KSeF-Feature", upoVersion } } : null,
			cancellationToken);
    }

    /// <inheritdoc />
    public Task CloseBatchSessionAsync(string batchSessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(batchSessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Sessions.Batch.Close(Uri.EscapeDataString(batchSessionReferenceNumber));
        return ExecuteAsync(endpoint, HttpMethod.Post, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default)
    {
        if (parts == null || parts.Count == 0)
        {
            throw new ArgumentException("Brak plików do wysłania.", nameof(parts));
        }

        return BatchPartsSender.SendPackagePartsAsync(
            restClient,
            openBatchSessionResponse.PartUploadRequests,
            parts,
            (info) => new ByteArrayContent(info.Data),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task SendBatchPartsWithStreamAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartStreamSendingInfo> parts, CancellationToken cancellationToken = default)
    {
        if (parts == null || parts.Count == 0)
        {
            throw new ArgumentException("Brak plików do wysłania.", nameof(parts));
        }

        return BatchPartsSender.SendPackagePartsAsync(
            restClient,
            openBatchSessionResponse.PartUploadRequests,
            parts,
            (info) => new StreamContent(info.DataStream),
            cancellationToken
        );
    }
}
