
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Invoices
{

    public class PagedInvoiceResponse
    {
        public bool HasMore { get; set; }
        public bool IsTruncated { get; set; }
        public ICollection<InvoiceSummary> Invoices { get; set; }
    }
}
