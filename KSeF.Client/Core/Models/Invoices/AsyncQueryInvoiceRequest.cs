
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Core.Models.Invoices;

public class AsyncQueryInvoiceRequest : QueryInvoiceRequest
{
   public EncryptionInfo Encryption { get; set; }
}
