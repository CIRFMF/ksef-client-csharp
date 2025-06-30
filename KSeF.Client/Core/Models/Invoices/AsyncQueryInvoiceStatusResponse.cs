
namespace KSeF.Client.Core.Models.Invoices;

public class AsyncQueryInvoiceStatusResponse : OperationStatusResponse
{
    public InvoicePackageParts PackageParts { get; set; }
}
