using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions;

public interface IOpenBatchSessionRequestBuilder
{
    IOpenBatchSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value);
}

public interface IOpenBatchSessionRequestBuilderWithFormCode
{
    IOpenBatchSessionRequestBuilderBatchFile WithBatchFile(long fileSize, string fileHash);
    IOpenBatchSessionRequestBuilderWithFormCode WithOfflineMode(bool offlineMode = false);
}

public interface IOpenBatchSessionRequestBuilderBatchFile
{
    IOpenBatchSessionRequestBuilderBatchFile AddBatchFilePart(string fileName, int ordinalNumber, long fileSize, string fileHash);
    IOpenBatchSessionRequestBuilderEncryption EndBatchFile();
}

public interface IOpenBatchSessionRequestBuilderEncryption
{
    IOpenBatchSessionRequestBuilderBuild WithEncryption(string encryptedSymmetricKey, string initializationVector);
}

public interface IOpenBatchSessionRequestBuilderBuild
{
    OpenBatchSessionRequest Build();
}

internal class OpenBatchSessionRequestBuilderImpl
    : IOpenBatchSessionRequestBuilder
    , IOpenBatchSessionRequestBuilderWithFormCode
    , IOpenBatchSessionRequestBuilderBatchFile
    , IOpenBatchSessionRequestBuilderEncryption
    , IOpenBatchSessionRequestBuilderBuild
{
    private FormCode _formCode;
    private readonly List<BatchFilePartInfo> _parts = new();
    private long _batchFileSize;
    private string _batchFileHash = "";
    private bool _offlineMode = false;
    private EncryptionInfo _encryption = new();

    private OpenBatchSessionRequestBuilderImpl() { }

    public static IOpenBatchSessionRequestBuilder Create() => new OpenBatchSessionRequestBuilderImpl();

    public IOpenBatchSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value)
    {
        if (string.IsNullOrWhiteSpace(systemCode) || string.IsNullOrWhiteSpace(schemaVersion) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Parametry FormCode nie mogą być puste ani null.");

        _formCode = new FormCode
        {
            SystemCode = systemCode,
            SchemaVersion = schemaVersion,
            Value = value
        };
        return this;
    }

    public IOpenBatchSessionRequestBuilderBatchFile WithBatchFile(long fileSize, string fileHash)
    {
        if (fileSize < 0 || string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("Parametry BatchFile są nieprawidłowe.");

        _batchFileSize = fileSize;
        _batchFileHash = fileHash;
        return this;
    }

    public IOpenBatchSessionRequestBuilderBatchFile AddBatchFileParts(
        IEnumerable<(string fileName, int ordinalNumber, long fileSize, string fileHash)> parts)
    {
        foreach ((string fileName, int ordinalNumber, long fileSize, string fileHash) part in parts)
        {
            AddBatchFilePart(part.fileName, part.ordinalNumber, part.fileSize, part.fileHash);
        }
        return this;
    }
    
    public IOpenBatchSessionRequestBuilderBatchFile AddBatchFilePart(string fileName, int ordinalNumber, long fileSize, string fileHash)
    {
        if (string.IsNullOrWhiteSpace(fileName) || ordinalNumber < 0 || fileSize < 0 || string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("Parametry BatchFilePart są nieprawidłowe.");

        _parts.Add(new BatchFilePartInfo
        {
            OrdinalNumber = ordinalNumber,
            FileSize = fileSize,
            FileHash = fileHash,
            FileName = fileName
        });
        return this;
    }

    public IOpenBatchSessionRequestBuilderEncryption EndBatchFile()
    {
        if (string.IsNullOrWhiteSpace(_batchFileHash))
            throw new InvalidOperationException("Hash BatchFile musi być ustawiony.");
        return this;
    }

    public IOpenBatchSessionRequestBuilderBuild WithEncryption(string encryptedSymmetricKey, string initializationVector)
    {
        if (string.IsNullOrWhiteSpace(encryptedSymmetricKey) || string.IsNullOrWhiteSpace(initializationVector))
            throw new ArgumentException("Parametry szyfrowania nie mogą być puste ani null.");

        _encryption.EncryptedSymmetricKey = encryptedSymmetricKey;
        _encryption.InitializationVector = initializationVector;
        return this;
    }
    public IOpenBatchSessionRequestBuilderWithFormCode WithOfflineMode(bool offlineMode = false)
    {
        _offlineMode = offlineMode;
        return this;
    }

    public OpenBatchSessionRequest Build()
    {
        if (_formCode == null) throw new InvalidOperationException("FormCode jest wymagany.");
        if (string.IsNullOrWhiteSpace(_encryption.EncryptedSymmetricKey) || string.IsNullOrWhiteSpace(_encryption.InitializationVector))
            throw new InvalidOperationException("Konfiguracja szyfrowania jest niekompletna.");

        return new OpenBatchSessionRequest
        {
            FormCode = _formCode,
            BatchFile = new BatchFileInfo
            {
                FileSize = _batchFileSize,
                FileHash = _batchFileHash,
                FileParts = _parts
            },
            OfflineMode = _offlineMode,
            Encryption = _encryption
        };
    }
}

public static class OpenBatchSessionRequestBuilder
{
    public static IOpenBatchSessionRequestBuilder Create() =>
        OpenBatchSessionRequestBuilderImpl.Create();
}