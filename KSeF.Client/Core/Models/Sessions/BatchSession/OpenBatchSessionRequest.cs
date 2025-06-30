namespace KSeF.Client.Core.Models.Sessions.BatchSession;

public class OpenBatchSessionRequest
{
    public FormCode FormCode { get; set; }
    public BatchFileInfo BatchFile { get; set; }
    public EncryptionInfo Encryption { get; set; }
    public bool OfflineMode { get; set; } = false;
}
