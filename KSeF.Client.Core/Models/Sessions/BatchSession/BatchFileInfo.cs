using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions.BatchSession
{
    /// <summary>
    /// Zawiera informacje o pliku wsadowym przekazywanym w żądaniu otwarcia sesji wsadowej.
    /// </summary>
    public class BatchFileInfo
    {
        /// <summary>
        /// Rozmiar całego pliku wsadowego w bajtach.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Skrót kryptograficzny całego pliku wsadowego.
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// Typ kompresji pliku wsadowego.
        /// Gdy wartość nie została podana, pozostaje <c>null</c> dla zachowania kompatybilności wstecznej.
        /// </summary>
        public CompressionType? CompressionType { get; set; }

        /// <summary>
        /// Lista części pliku wsadowego.
        /// </summary>
        public ICollection<BatchFilePartInfo> FileParts { get; set; }
    }
}
