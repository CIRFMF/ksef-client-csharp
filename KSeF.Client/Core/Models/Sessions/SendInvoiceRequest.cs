namespace KSeF.Client.Core.Models.Sessions;

public class SendInvoiceRequest
{
    public string InvoiceHash { get; set; }
    public long InvoiceSize { get; set; }

    public string EncryptedInvoiceHash { get; set; }
    public long EncryptedInvoiceSize { get; set; }

    public string EncryptedInvoiceContent { get; set; }

    public bool OfflineMode { get; set; } = false;

    public string HashOfCorrectedInvoice { get; set; } 
}
