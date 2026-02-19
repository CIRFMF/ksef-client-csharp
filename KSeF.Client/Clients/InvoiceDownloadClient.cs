using System.Text;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Http.Helpers;

namespace KSeF.Client.Clients;

/// <inheritdoc />
public class InvoiceDownloadClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IInvoiceDownloadClient
{
    /// <inheritdoc />
    public Task<string> GetInvoiceAsync(string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(ksefNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Invoices.ByKsefNumber(Uri.EscapeDataString(ksefNumber));

        Dictionary<string, string> headers = new()
        {
            ["Accept"] = "application/xml"
        };

        return ExecuteAsync<string>(endpoint, HttpMethod.Get, accessToken, headers, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedInvoiceResponse> QueryInvoiceMetadataAsync(
        InvoiceQueryFilters requestPayload,
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        SortOrder sortOrder = SortOrder.Asc,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new StringBuilder(Routes.Invoices.QueryMetadata).Append("?sortOrder=").Append(sortOrder);
        PaginationHelper.AppendPagination(pageOffset, pageSize, urlBuilder);

        return ExecuteAsync<PagedInvoiceResponse, InvoiceQueryFilters>(
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationResponse> ExportInvoicesAsync(
    InvoiceExportRequest requestPayload,
    string accessToken,
    bool includeMetadata = true,
    CancellationToken cancellationToken = default)
    {
        return ExportInvoicesAsync(requestPayload, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> ExportInvoicesAsync(
        InvoiceExportRequest requestPayload,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Invoices.Exports;


        return await ExecuteAsync<OperationResponse, InvoiceExportRequest>(
            endpoint,
            requestPayload,
            accessToken,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<InvoiceExportStatusResponse> GetInvoiceExportStatusAsync(
        string referenceNumber,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(referenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        string endpoint = Routes.Invoices.ExportByReference(Uri.EscapeDataString(referenceNumber));

        return ExecuteAsync<InvoiceExportStatusResponse>(endpoint, HttpMethod.Get, accessToken, cancellationToken);
    }
}
