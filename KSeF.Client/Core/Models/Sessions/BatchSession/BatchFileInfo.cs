namespace KSeF.Client.Core.Models.Sessions.BatchSession;


public class BatchFileInfo
{
    public long FileSize { get; set; }
    public string FileHash { get; set; }
    public ICollection<BatchFilePartInfo> FileParts { get; set; }

}

