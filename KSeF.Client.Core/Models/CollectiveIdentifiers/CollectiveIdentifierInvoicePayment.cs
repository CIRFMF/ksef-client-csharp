using KSeF.Client.Core.Models.Invoices.Common;

namespace KSeF.Client.Core.Models.CollectiveIdentifiers
{
    public class CollectiveIdentifierInvoicePayment
    {
        public decimal Amount { get; set; }
        public CurrencyCode Currency { get; set; }
    }
}
