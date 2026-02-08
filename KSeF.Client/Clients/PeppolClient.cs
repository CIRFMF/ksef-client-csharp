using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Peppol;
using System.Text;
using KSeF.Client.Http.Helpers;

namespace KSeF.Client.Clients;

/// <summary>
/// Implementacja klienta Peppol oparta o ClientBase.
/// </summary>
public class PeppolClient(IRestClient restClient, IRouteBuilder routeBuilder) : ClientBase(restClient, routeBuilder), IPeppolClient
{
    /// <inheritdoc />
    public Task<QueryPeppolProvidersResponse> QueryPeppolProvidersAsync(
        string accessToken,
        int? pageOffset = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        StringBuilder urlBuilder = new(Routes.Peppol.Query);

        PaginationHelper.AppendPagination(pageOffset, pageSize, urlBuilder);

        return ExecuteAsync<QueryPeppolProvidersResponse>(urlBuilder.ToString(), HttpMethod.Get, accessToken, cancellationToken);
    }
}
