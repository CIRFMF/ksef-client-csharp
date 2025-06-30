namespace KSeF.Client.Core.Models.Sessions;


public class SessionInvoicesResponse
{
    public string ContinuationToken { get; set; }

    public ICollection<SessionInvoice> Invoices { get; set; }

    public int TotalCount { get; set; }

}