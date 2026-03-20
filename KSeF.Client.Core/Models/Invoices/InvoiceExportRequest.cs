using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Core.Models.Invoices
{
    /// <summary>
    /// Request do inicjacji eksportu paczki faktur.
    /// </summary>
    public class InvoiceExportRequest
    {
        /// <summary>
        /// Informacje o szyfrowaniu paczki (RSA OAEP-SHA256, Base64).
        /// </summary>
        public EncryptionInfo Encryption { get; set; }

		/// <summary>
		/// Określa, czy zwrócić tylko metadane faktur (plik _metadata.json bez faktur).
		/// </summary>
		public bool OnlyMetadata{ get; set; }

        /// <summary>
        /// Filtry wyboru faktur do eksportu (dedykowany model dla eksportu).
        /// </summary>
        public InvoiceQueryFilters Filters { get; set; }
    }
}
