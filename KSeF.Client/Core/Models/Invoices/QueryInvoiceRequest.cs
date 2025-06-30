
using System.ComponentModel.DataAnnotations;

namespace KSeF.Client.Core.Models.Invoices;

public class QueryInvoiceRequest
{
    [Required]
    public SubjectType SubjectType { get; set; }
    [Required]
    public DateRange DateRange { get; set; }
    public string KsefNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public AmountFilter Amount { get; set; }
    public PartyInfo Seller { get; set; }
    public BuyerInfo Buyer { get; set; }
    public List<string> CurrencyCodes { get; set; }
    public bool IsHidden { get; set; }
    public InvoicingMode InvoicingMode { get; set; }
    public bool IsSelfInvoicing { get; set; }
    public string InvoiceSchema { get; set; }
    public ICollection<InvoiceType> InvoiceTypes { get; set; }
}
