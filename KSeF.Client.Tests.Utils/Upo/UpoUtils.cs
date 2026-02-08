#nullable enable
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Infrastructure.Rest;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;

namespace KSeF.Client.Tests.Utils.Upo;

public static class UpoUtils
{
    /// <summary>
    /// Deserializuje XML UPO do obiektu typu T, gdzie T implementuje IUpoParsable.
    /// </summary>
    /// <param name="xml">Pełna reprezentacja XML zgodna ze schematem UPO.</param>
    /// <returns>Wynik deserializacji.</returns>
    /// <exception cref="ArgumentNullException">Gdy parametr xml jest null.</exception>
    /// <exception cref="InvalidOperationException">Gdy deserializacja się nie powiedzie (np. niezgodność elementów/namespaces).</exception>
    public static T UpoParse<T>(string xml) where T : IUpoParsable
    {
        XmlSerializer serializer = new(typeof(T));
        using StringReader stringReader = new(xml);
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using XmlReader xmlReader = XmlReader.Create(stringReader, settings);

        return (T)serializer.Deserialize(xmlReader)!;
    }

    /// <summary>
    /// Pobiera UPO faktury z sesji na podstawie jej numeru KSeF.
    /// </summary>
    public static async Task<string> GetSessionInvoiceUpoAsync(IKSeFClient ksefClient, string sessionReferenceNumber, string ksefNumber, string accessToken)
    {
        return await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefNumber, accessToken, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera zbiorcze UPO sesji na podstawie numeru referencyjnego UPO.
    /// </summary>
    public static async Task<string> GetSessionUpoAsync(IKSeFClient ksefClient, string sessionReferenceNumber, string upoReferenceNumber, string accessToken)
    {
        return await ksefClient.GetSessionUpoAsync(sessionReferenceNumber, upoReferenceNumber, accessToken, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera UPO z adresu Uri.
    /// </summary>
    public static async Task<string> GetUpoAsync(IKSeFClient ksefClient, Uri uri)
    {
        return await ksefClient.GetUpoAsync(uri, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera UPO z adresu Uri razem z wartością nagłówka x-ms-meta-hash.
    /// </summary>
    public static async Task<UpoWithHash> GetUpoWithHashAsync(IRestClient restClient, Uri uri, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(restClient);
        Guard.ThrowIfNull(uri);

        RestResponse<string> response = await restClient
            .SendWithHeadersAsync<string, object>(
                HttpMethod.Get,
                uri.ToString(),
                requestBody: null,
                token: null,
                contentType: KSeF.Client.Http.RestClient.XmlContentType,
                additionalHeaders: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        response.TryGetHeaderSingle("x-ms-meta-hash", out string hashHeaderBase64);

        return new UpoWithHash
        {
            Xml = response.Body,
            HashHeaderBase64 = hashHeaderBase64
        };
    }

    private static async Task<UpoWithHash> GetUpoFromRelativePathAsync(
        IRestClient restClient,
        string relativePath,
        string accessToken,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<string>>? fallbackFetcher = null)
    {
        Guard.ThrowIfNull(restClient);
        Guard.ThrowIfNullOrWhiteSpace(relativePath);
        Guard.ThrowIfNullOrWhiteSpace(accessToken);

        RestResponse<string> response = await restClient
            .SendWithHeadersAsync<string, object>(
                HttpMethod.Get,
                relativePath,
                requestBody: null,
                token: accessToken,
                contentType: KSeF.Client.Http.RestClient.XmlContentType,
                additionalHeaders: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Accept"] = KSeF.Client.Http.RestClient.XmlContentType
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        response.TryGetHeaderSingle("x-ms-meta-hash", out string hashHeaderBase64);

        string xml = response.Body;
        if (string.IsNullOrWhiteSpace(xml) && fallbackFetcher is not null)
        {
            xml = await fallbackFetcher(cancellationToken).ConfigureAwait(false);
        }

        return new UpoWithHash
        {
            Xml = xml,
            HashHeaderBase64 = hashHeaderBase64
        };
    }

    /// <summary>
    /// Pobiera UPO faktury po numerze KSeF wraz z nagłówkiem x-ms-meta-hash.
    /// </summary>
    public static Task<UpoWithHash> GetSessionInvoiceUpoByKsefNumberWithHashAsync(
        IRestClient restClient,
        string sessionReferenceNumber,
        string ksefNumber,
        string accessToken,
        CancellationToken cancellationToken = default,
        IKSeFClient? ksefClientFallback = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(ksefNumber);

        string path = $"/v2/sessions/{Uri.EscapeDataString(sessionReferenceNumber)}/invoices/ksef/{Uri.EscapeDataString(ksefNumber)}/upo";

        return GetUpoFromRelativePathAsync(
            restClient,
            path,
            accessToken,
            cancellationToken,
            ksefClientFallback is null
                ? null
                : ct => ksefClientFallback.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefNumber, accessToken, ct));
    }

    /// <summary>
    /// Pobiera UPO faktury po numerze referencyjnym faktury wraz z nagłówkiem x-ms-meta-hash.
    /// </summary>
    public static Task<UpoWithHash> GetSessionInvoiceUpoByReferenceNumberWithHashAsync(
        IRestClient restClient,
        string sessionReferenceNumber,
        string invoiceReferenceNumber,
        string accessToken,
        CancellationToken cancellationToken = default,
        IKSeFClient? ksefClientFallback = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        Guard.ThrowIfNullOrWhiteSpace(invoiceReferenceNumber);

        string path = $"/v2/sessions/{Uri.EscapeDataString(sessionReferenceNumber)}/invoices/{Uri.EscapeDataString(invoiceReferenceNumber)}/upo";

        return GetUpoFromRelativePathAsync(
            restClient,
            path,
            accessToken,
            cancellationToken,
            ksefClientFallback is null
                ? null
                : ct => ksefClientFallback.GetSessionInvoiceUpoByReferenceNumberAsync(sessionReferenceNumber, invoiceReferenceNumber, accessToken, ct));
    }
}