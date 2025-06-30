namespace KSeF.Client.Core.Models.Sessions.BatchSession;

public class BatchPartSendingInfo
{
    public byte[] Data { get; set; }
    public FileMetadata Metadata { get; set; }
    public int OrdinalNumber { get; set; }
}
