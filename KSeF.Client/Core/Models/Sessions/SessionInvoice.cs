using KSeFClient.Core.Models;

namespace KSeF.Client.Core.Models.Sessions;


public class SessionInvoice
{
    public int OrdinalNumber { get; set; }

    public string InvoiceNumber { get; set; }

    public string KsefNumber { get; set; }

    public string ReferenceNumber { get; set; }

    public string InvoiceHash { get; set; }

    public DateTimeOffset ReceiveDate { get; set; }

    public StatusInfo Status { get; set; }
}
