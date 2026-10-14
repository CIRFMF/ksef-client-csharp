using System.Text;

namespace KSeF.Client.Http.Helpers;

internal static class UrlExtensions
{
    /// <summary>
    /// Łączy bazowy adres URL ze ścieżką, zachowując ścieżkę bazową (np. /api/).
    /// W przeciwieństwie do new Uri(base, relative), nie odrzuca ścieżki bazowej
    /// gdy ścieżka zaczyna się od '/'.
    /// </summary>
    public static string Combine(this Uri baseAddress, string path)
    {
        string baseUri = baseAddress?.AbsoluteUri.TrimEnd('/');
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) {
            // When no base address is configured, there is no host to validate against, so allow the absolute path through.
            if (baseAddress is null) {
                return path;
            }
            
            // Only allow an absolute path if it targets the same host as the configured base address,
            // preventing the request from being redirected to an attacker-controlled server (SSRF).
            if (baseAddress != null && Uri.TryCreate(path, UriKind.Absolute, out Uri absolutePath) && Uri.Compare(absolutePath, baseAddress, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0) {
                return path;
            } else {
                throw new InvalidOperationException($"Request path [{path}] does not match the configured base address and was rejected.");
            }
        } else {
            return baseUri is null ? path : baseUri + "/" + path.TrimStart('/');
        }
    }

    public static string WithQuery(this string path, IDictionary<string, string> query, Uri baseAddress)
    {
        string uri = baseAddress.Combine(path);

        if (query == null || query.Count == 0)
        {
            return uri;
        }

        StringBuilder builder = new(uri);
        builder.Append(uri.Contains('?') ? "&" : "?");

        bool first = true;
        foreach (KeyValuePair<string, string> pair in query)
        {
            if (!first)
            {
                builder.Append('&');
            }

            first = false;
            string name = Uri.EscapeDataString(pair.Key);
            string value = pair.Value is null ? string.Empty : Uri.EscapeDataString(pair.Value);
            builder.Append(name).Append('=').Append(value);
        }
        return builder.ToString();
    }
}

