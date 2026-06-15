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
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            return path;

        string baseUri = baseAddress?.AbsoluteUri.TrimEnd('/');
        return baseUri is null ? path : baseUri + "/" + path.TrimStart('/');
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

