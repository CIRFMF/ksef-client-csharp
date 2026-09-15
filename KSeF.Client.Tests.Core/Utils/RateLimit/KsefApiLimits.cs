#nullable enable
namespace KSeF.Client.Tests.Core.Utils.RateLimit;

/// <summary>
/// Mapa limitów API KSeF
/// </summary>
public static class KsefApiLimits
{
    // Profile limitów - nazwane wg grup zwracanych przez GET /rate-limits (EffectiveApiRateLimits)
    private static readonly ApiLimits InvoiceMetadata = new() { RequestsPerSecond = 8, RequestsPerMinute = 16, RequestsPerHour = 20 };
    private static readonly ApiLimits InvoiceExportLimits = new() { RequestsPerSecond = 8, RequestsPerMinute = 16, RequestsPerHour = 20 };
    private static readonly ApiLimits InvoiceExportStatus = new() { RequestsPerSecond = 10, RequestsPerMinute = 60, RequestsPerHour = 600 };
    private static readonly ApiLimits InvoiceDownload = new() { RequestsPerSecond = 8, RequestsPerMinute = 16, RequestsPerHour = 64 };
    private static readonly ApiLimits BatchSession = new() { RequestsPerSecond = 10, RequestsPerMinute = 20, RequestsPerHour = 60 };
    private static readonly ApiLimits BatchSessionClose = new() { RequestsPerSecond = 20, RequestsPerMinute = 40, RequestsPerHour = 120 };
    private static readonly ApiLimits OnlineSession = new() { RequestsPerSecond = 10, RequestsPerMinute = 30, RequestsPerHour = 120 };
    private static readonly ApiLimits OnlineSessionClose = new() { RequestsPerSecond = 20, RequestsPerMinute = 60, RequestsPerHour = 240 };
    private static readonly ApiLimits InvoiceSend = new() { RequestsPerSecond = 10, RequestsPerMinute = 30, RequestsPerHour = 180 };
    private static readonly ApiLimits InvoiceStatus = new() { RequestsPerSecond = 30, RequestsPerMinute = 120, RequestsPerHour = 1200 };
    private static readonly ApiLimits SessionList = new() { RequestsPerSecond = 5, RequestsPerMinute = 10, RequestsPerHour = 60 };
    private static readonly ApiLimits SessionInvoiceList = new() { RequestsPerSecond = 10, RequestsPerMinute = 20, RequestsPerHour = 200 };
    private static readonly ApiLimits SessionMisc = new() { RequestsPerSecond = 10, RequestsPerMinute = 120, RequestsPerHour = 1200 };
    private static readonly ApiLimits CollectiveIdentifier = new() { RequestsPerSecond = 20, RequestsPerMinute = 120, RequestsPerHour = 240 };
    private static readonly ApiLimits Other = new() { RequestsPerSecond = 10, RequestsPerMinute = 30, RequestsPerHour = 120 };

    private static readonly Dictionary<KsefApiEndpoint, ApiLimits> _limits = new()
    {
        [KsefApiEndpoint.InvoiceQueryMetadata] = InvoiceMetadata,
        [KsefApiEndpoint.InvoiceExport] = InvoiceExportLimits,
        [KsefApiEndpoint.InvoiceExportStatus] = InvoiceExportStatus,
        [KsefApiEndpoint.InvoiceGetByNumber] = InvoiceDownload,
        [KsefApiEndpoint.SessionBatchOpen] = BatchSession,
        [KsefApiEndpoint.SessionBatchClose] = BatchSessionClose,
        [KsefApiEndpoint.SessionOnlineOpen] = OnlineSession,
        [KsefApiEndpoint.SessionOnlineSendInvoice] = InvoiceSend,
        [KsefApiEndpoint.SessionOnlineClose] = OnlineSessionClose,
        [KsefApiEndpoint.SessionInvoiceStatus] = InvoiceStatus,
        [KsefApiEndpoint.SessionList] = SessionList,
        [KsefApiEndpoint.SessionInvoiceList] = SessionInvoiceList,
        [KsefApiEndpoint.SessionMisc] = SessionMisc,
        [KsefApiEndpoint.CollectiveIdentifier] = CollectiveIdentifier,
        [KsefApiEndpoint.Other] = Other
    };
    
    /// <summary>
    /// Zwraca limity konkretnego endpointu API.
    /// </summary>
    public static ApiLimits GetLimits(KsefApiEndpoint endpoint)
    {
        return _limits.TryGetValue(endpoint, out ApiLimits? limits) ? limits : _limits[KsefApiEndpoint.Other];
    }
    
    /// <summary>
    /// Zwraca wszystkie zdefiniowane limity (dla celów diagnostycznych).
    /// </summary>
    public static IReadOnlyDictionary<KsefApiEndpoint, ApiLimits> GetAllLimits()
    {
#if NETFRAMEWORK
        return new System.Collections.ObjectModel.ReadOnlyDictionary<KsefApiEndpoint, ApiLimits>(_limits);
#else
        return _limits.AsReadOnly();
#endif
    }
}