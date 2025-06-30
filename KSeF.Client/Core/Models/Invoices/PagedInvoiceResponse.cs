
namespace KSeF.Client.Core.Models.Invoices;


public class PagedInvoiceResponse
{
    public int PageOffset { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public ICollection<InvoiceSummary> Invoices { get; set; }
}
