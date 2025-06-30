namespace KSeF.Client.Core.Models.Sessions;

public class EncryptionKey
{
    public string Encoding { get; set; }
    public string Algorithm { get; set; }
    public int Size { get; set; }
    public string Value { get; set; }
}

