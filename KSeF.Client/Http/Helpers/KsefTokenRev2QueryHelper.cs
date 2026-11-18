using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Core.Models.Token;
using System.Text;

namespace KSeF.Client.Http.Helpers;

/// <summary>
/// Buduje adres zapytania o listę tokenów KSeF w kontrakcie Rev2 (GET /tokens/rev2).
/// </summary>
internal static class KsefTokenRev2QueryHelper
{
    internal static string BuildUrl(
        string basePath,
        ICollection<KsefTokenStatusFilterRev2> statuses,
        string authorIdentifier,
        TokenContextIdentifierType? authorIdentifierType,
        string description,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        int? pageSize)
    {
        StringBuilder urlBuilder = new(basePath);

        void Append(string name, string value)
        {
            urlBuilder.Append(urlBuilder.ToString().Contains('?') ? '&' : '?');
            urlBuilder.Append(name).Append('=').Append(Uri.EscapeDataString(value));
        }

        if (statuses is { Count: > 0 })
        {
            foreach (KsefTokenStatusFilterRev2 status in statuses)
            {
                Append("status", status.ToString());
            }
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            Append("description", description);
        }

        if (!string.IsNullOrWhiteSpace(authorIdentifier))
        {
            Append("authorIdentifier", authorIdentifier);
        }

        if (authorIdentifierType.HasValue)
        {
            Append("authorIdentifierType", authorIdentifierType.Value.ToString());
        }

        if (validFrom.HasValue)
        {
            Append("validFrom", validFrom.Value.ToString("o"));
        }

        if (validTo.HasValue)
        {
            Append("validTo", validTo.Value.ToString("o"));
        }

        PaginationHelper.AppendPagination(null, pageSize, urlBuilder);

        return urlBuilder.ToString();
    }
}
