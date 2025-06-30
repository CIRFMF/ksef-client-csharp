namespace KSeF.Client.Core.Models.Sessions;

public class EncryptionData
{
    public byte[] CipherKey { get; init; }
    public byte[] CipherIv { get; init; }

    public string EncryptedCipherKey { get; init; }
    public EncryptionInfo EncryptionInfo { get; init; }
}
