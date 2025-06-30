using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Core.Models.Sessions;

public interface IOpenOnlineSessionRequestBuilder
{
    IOpenOnlineSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value);
}

public interface IOpenOnlineSessionRequestBuilderWithFormCode
{
    IOpenOnlineSessionRequestBuilderWithEncryption WithEncryption(string encryptedSymmetricKey, string initializationVector);
}

public interface IOpenOnlineSessionRequestBuilderWithEncryption
{
    OpenOnlineSessionRequest Build();
}

internal class OpenOnlineSessionRequestBuilderImpl
    : IOpenOnlineSessionRequestBuilder
    , IOpenOnlineSessionRequestBuilderWithFormCode
    , IOpenOnlineSessionRequestBuilderWithEncryption
{
    private FormCode _formCode;
    private EncryptionInfo _encryption = new();

    private OpenOnlineSessionRequestBuilderImpl() { }

    public static IOpenOnlineSessionRequestBuilder Create() => new OpenOnlineSessionRequestBuilderImpl();

    public IOpenOnlineSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value)
    {
        if (string.IsNullOrWhiteSpace(systemCode) || string.IsNullOrWhiteSpace(schemaVersion) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FormCode parameters cannot be null or empty.");

        _formCode = new FormCode
        {
            SystemCode = systemCode,
            SchemaVersion = schemaVersion,
            Value = value
        };
        return this;
    }

    public IOpenOnlineSessionRequestBuilderWithEncryption WithEncryption(string encryptedSymmetricKey, string initializationVector)
    {
        if (string.IsNullOrWhiteSpace(encryptedSymmetricKey) || string.IsNullOrWhiteSpace(initializationVector))
            throw new ArgumentException("Encryption parameters cannot be null or empty.");

        _encryption.EncryptedSymmetricKey = encryptedSymmetricKey;
        _encryption.InitializationVector = initializationVector;
        return this;
    }

    public OpenOnlineSessionRequest Build()
    {
        if (_formCode == null) throw new InvalidOperationException("FormCode is required.");
        if (string.IsNullOrWhiteSpace(_encryption.EncryptedSymmetricKey) || string.IsNullOrWhiteSpace(_encryption.InitializationVector))
            throw new InvalidOperationException("Encryption configuration is incomplete.");

        return new OpenOnlineSessionRequest
        {
            FormCode = _formCode,
            Encryption = _encryption
        };
    }
}

public static class OpenOnlineSessionRequestBuilder
{
    public static IOpenOnlineSessionRequestBuilder Create() =>
        OpenOnlineSessionRequestBuilderImpl.Create();
}
