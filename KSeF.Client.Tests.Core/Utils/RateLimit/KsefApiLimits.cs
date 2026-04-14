#nullable enable
namespace KSeF.Client.Tests.Core.Utils.RateLimit;

/// <summary>
/// Mapa limitów API KSeF
/// </summary>
public static class KsefApiLimits
{
    // Predefiniowane profile limitów
    private static readonly ApiLimits Medium = new() { RequestsPerSecond = 8, RequestsPerMinute = 16, RequestsPerHour = 20 };
    private static readonly ApiLimits Standard = new() { RequestsPerSecond = 10, RequestsPerMinute = 20, RequestsPerHour = 120 };
    private static readonly ApiLimits Enhanced = new() { RequestsPerSecond = 10, RequestsPerMinute = 30, RequestsPerHour = 120 };
    private static readonly ApiLimits High = new() { RequestsPerSecond = 30, RequestsPerMinute = 120, RequestsPerHour = 720 };
    
    private static readonly Dictionary<KsefApiEndpoint, ApiLimits> _limits = new()
    {
        [KsefApiEndpoint.InvoiceQueryMetadata] = Medium,
        [KsefApiEndpoint.InvoiceExport] = Medium,
        [KsefApiEndpoint.InvoiceGetByNumber] = Medium with { RequestsPerHour = 64 },
        [KsefApiEndpoint.SessionBatchOpen] = Standard,
        [KsefApiEndpoint.SessionBatchClose] = Standard,
        [KsefApiEndpoint.SessionOnlineOpen] = Enhanced,
        [KsefApiEndpoint.SessionOnlineSendInvoice] = Enhanced with { RequestsPerHour = 180 },
        [KsefApiEndpoint.SessionOnlineClose] = Enhanced,
        [KsefApiEndpoint.SessionInvoiceStatus] = High,
        [KsefApiEndpoint.Other] = Enhanced
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