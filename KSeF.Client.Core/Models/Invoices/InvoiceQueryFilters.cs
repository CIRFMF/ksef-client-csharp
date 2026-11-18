
using KSeF.Client.Core.Models.Invoices.Common;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Invoices
{
    public class InvoiceQueryFilters
    {
        public InvoiceSubjectType SubjectType { get; set; }
        public DateRange DateRange { get; set; }
        public string KsefNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public AmountFilter Amount { get; set; }
        public string SellerNip { get; set; }
        public BuyerIdentifier BuyerIdentifier { get; set; }
        public List<CurrencyCode> CurrencyCodes { get; set; }
        public InvoicingMode? InvoicingMode { get; set; }
        public bool? IsSelfInvoicing { get; set; }
        public FormType? FormType { get; set; }
        public ICollection<InvoiceType> InvoiceTypes { get; set; }
        public bool? HasAttachment { get; set; }
    }
    
    public enum SortOrder
    {
        Asc,
        Desc
    }
    public enum FormType
    {
        FA,
        PEF,
        FA_RR
    }
}
