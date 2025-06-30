
namespace KSeF.Client.Core.Models.Invoices;

public class InvoiceMetadata
{
    public string Number { get; set; }
    public decimal TotalAmountDue { get; set; }
    public Buyer Buyer { get; set; }
}
