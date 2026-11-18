using KSeF.Client.Core.Models.Invoices;
using System;

namespace KSeF.Client.Core.Models.Sessions
{
    public class SessionInvoice
    {
        /// <summary>
        /// Numer sekwencyjny faktury w ramach sesji.
        /// </summary>
        public int OrdinalNumber { get; set; }

        /// <summary>
        /// Numer faktury (może być null).
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Numer KSeF (może być null).
        /// </summary>
        public string KsefNumber { get; set; }

        /// <summary>
        /// Numer referencyjny faktury (może być null).
        /// </summary>
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Skrót SHA256 faktury, zakodowany w Base64 (może być null).
        /// </summary>
        public string InvoiceHash { get; set; }

        /// <summary>
        /// Nazwa pliku faktury (dla faktur wsadowych, może być null).
        /// </summary>
        public string InvoiceFileName { get; set; }

        /// <summary>
        /// Data nadania numeru KSeF (może być null).
        /// </summary>
        public DateTimeOffset? AcquisitionDate { get; set; }

        /// <summary>
        /// Data przyjęcia faktury w systemie KSeF (wymagana).
        /// </summary>
        public DateTimeOffset InvoicingDate { get; set; }

        /// <summary>
        /// Data trwałego zapisu faktury w repozytorium KSeF (może być null).
        /// </summary>
        public DateTimeOffset? PermanentStorageDate { get; set; }

        /// <summary>
        /// Adres do pobrania UPO (może być null).
        /// </summary>
        public Uri UpoDownloadUrl { get; set; }

        /// <summary>
        /// Status faktury (wymagany obiekt).
        /// </summary>
        public InvoiceStatusInfo Status { get; set; }

        /// <summary>
        /// Tryb fakturowania.
        /// </summary>
        public InvoicingMode? InvoicingMode { get; set; }

        /// <summary>
        /// Data wygaśnięcia adresu do pobrania UPO.
        /// </summary>
        public DateTimeOffset? UpoDownloadUrlExpirationDate { get; set; }
    }
}