namespace KSeF.Client.Core.Models.RateLimits;

/// <summary>
/// Enum określający grupy limitów żądań API KSeF.
/// </summary>
public enum KsefClientRateLimitGroup
{
    /// <summary>
    /// Limity otwierania/zamykania sesji interaktywnych.
    /// </summary>
    OnlineSession = 0,

    /// <summary>
    /// Limity otwierania/zamykania sesji wsadowych.
    /// </summary>
    BatchSession = 1,

    /// <summary>
    /// Limity wysyłki faktur.
    /// </summary>
    InvoiceSend = 2,

    /// <summary>
    /// Limity pobierania statusu faktury z sesji.
    /// </summary>
    InvoiceStatus = 3,

    /// <summary>
    /// Limity pobierania listy sesji.
    /// </summary>
    SessionList = 4,

    /// <summary>
    /// Limity pobierania listy faktur w sesji.
    /// </summary>
    SessionInvoiceList = 5,

    /// <summary>
    /// Limity pozostałych operacji w ramach sesji.
    /// </summary>
    SessionMisc = 6,

    /// <summary>
    /// Limity pobierania metadanych faktur.
    /// </summary>
    InvoiceMetadata = 7,

    /// <summary>
    /// Limity eksportu paczki faktur.
    /// </summary>
    InvoiceExport = 8,

    /// <summary>
    /// Limity pobierania statusu eksportu paczki faktur.
    /// </summary>
    InvoiceExportStatus = 9,

    /// <summary>
    /// Limity pobierania faktur po numerze KSeF.
    /// </summary>
    InvoiceDownload = 10,

    /// <summary>
    /// Limity pozostałych operacji API.
    /// </summary>
    Other = 11,

    /// <summary>
    /// Limity generowania identyfikatorów zbiorczych.
    /// </summary>
    CollectiveIdentifier = 12
}
