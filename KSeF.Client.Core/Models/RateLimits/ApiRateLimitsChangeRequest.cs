namespace KSeF.Client.Core.Models.RateLimits
{
    /// <summary>
    /// Nadpisywalne limity żądań API używane w żądaniu POST /testdata/rate-limits.
    /// </summary>
    public class ApiRateLimitsChangeRequest
    {
        /// <summary>
        /// Limity otwierania sesji interaktywnych.
        /// </summary>
        public EffectiveApiRateLimitValues OnlineSession { get; set; }

        /// <summary>
        /// Limity otwierania sesji wsadowych.
        /// </summary>
        public EffectiveApiRateLimitValues BatchSession { get; set; }

        /// <summary>
        /// Limity wysyłki faktur.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceSend { get; set; }

        /// <summary>
        /// Limity pobierania statusu faktury z sesji.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceStatus { get; set; }

        /// <summary>
        /// Limity pobierania listy sesji.
        /// </summary>
        public EffectiveApiRateLimitValues SessionList { get; set; }

        /// <summary>
        /// Limity pobierania listy faktur w sesji.
        /// </summary>
        public EffectiveApiRateLimitValues SessionInvoiceList { get; set; }

        /// <summary>
        /// Limity pozostałych operacji w ramach sesji.
        /// </summary>
        public EffectiveApiRateLimitValues SessionMisc { get; set; }

        /// <summary>
        /// Limity pobierania metadanych faktur.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceMetadata { get; set; }

        /// <summary>
        /// Limity eksportu paczki faktur.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceExport { get; set; }

        /// <summary>
        /// Limity statusu eksportu paczki faktur.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceExportStatus { get; set; }

        /// <summary>
        /// Limity pobierania faktur po numerze KSeF.
        /// </summary>
        public EffectiveApiRateLimitValues InvoiceDownload { get; set; }

        /// <summary>
        /// Limity generowania identyfikatorów zbiorczych.
        /// </summary>
        public EffectiveApiRateLimitValues CollectiveIdentifier { get; set; }

        /// <summary>
        /// Limity pozostałych operacji API.
        /// </summary>
        public EffectiveApiRateLimitValues Other { get; set; }
    }
}
