using KSeF.Client.Core.Models.Sessions;

public interface ISendInvoiceOnlineSessionRequestBuilder
{
    ISendInvoiceOnlineSessionRequestBuilderWithDocumentHash WithDocumentHash(string documentHash, long documentSize);
}

public interface ISendInvoiceOnlineSessionRequestBuilderWithDocumentHash
{
    ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize);
}

public interface ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
{
    ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent);
}

public interface ISendInvoiceOnlineSessionRequestBuilderBuild
{
    ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice);
    ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode);
    SendInvoiceRequest Build();
}

internal class SendInvoiceOnlineSessionRequestBuilderImpl
    : ISendInvoiceOnlineSessionRequestBuilder
    , ISendInvoiceOnlineSessionRequestBuilderWithDocumentHash
    , ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
    , ISendInvoiceOnlineSessionRequestBuilderBuild
{
    private string _documentHash;
    private long _documentSize;
    private string _encryptedDocumentHash;
    private long _encryptedDocumentSize;
    private string _encryptedDocumentContent;
    private string _hashOfCorrectedInvoice;
    private bool _offlineMode = false;

    private SendInvoiceOnlineSessionRequestBuilderImpl() { }

    public static ISendInvoiceOnlineSessionRequestBuilder Create() => new SendInvoiceOnlineSessionRequestBuilderImpl();

    public ISendInvoiceOnlineSessionRequestBuilderWithDocumentHash WithDocumentHash(string documentHash, long documentSize)
    {
        if (string.IsNullOrWhiteSpace(documentHash) || documentSize < 0)
            throw new ArgumentException("InvoiceHash parameters are invalid.");

        _documentHash = documentHash;
        _documentSize = documentSize;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize)
    {
        if (string.IsNullOrWhiteSpace(encryptedDocumentHash) || encryptedDocumentSize < 0)
            throw new ArgumentException("EncryptedInvoiceHash parameters are invalid.");

        _encryptedDocumentHash = encryptedDocumentHash;
        _encryptedDocumentSize = encryptedDocumentSize;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent)
    {
        if (string.IsNullOrWhiteSpace(encryptedDocumentContent))
            throw new ArgumentException("EncryptedInvoiceContent cannot be null or empty.");

        _encryptedDocumentContent = encryptedDocumentContent;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice)
    {
        if (string.IsNullOrWhiteSpace(hashOfCorrectedInvoice))
            throw new ArgumentException("HashOfCorrectedInvoice cannot be null or empty.");

        _hashOfCorrectedInvoice = hashOfCorrectedInvoice;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode)
    {
        _offlineMode = offlineMode;
        return this;
    }

    public SendInvoiceRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_documentHash))
            throw new InvalidOperationException("InvoiceHash is required.");
        if (string.IsNullOrWhiteSpace(_encryptedDocumentHash))
            throw new InvalidOperationException("EncryptedInvoiceHash is required.");
        if (string.IsNullOrWhiteSpace(_encryptedDocumentContent))
            throw new InvalidOperationException("EncryptedInvoiceContent is required.");

        return new SendInvoiceRequest
        {
            InvoiceHash = _documentHash,
            InvoiceSize = _documentSize,
            EncryptedInvoiceHash = _encryptedDocumentHash,
            EncryptedInvoiceSize = _encryptedDocumentSize,
            EncryptedInvoiceContent = _encryptedDocumentContent,
            HashOfCorrectedInvoice = _hashOfCorrectedInvoice,
            OfflineMode = _offlineMode
        };
    }
}

public static class SendInvoiceOnlineSessionRequestBuilder
{
    public static ISendInvoiceOnlineSessionRequestBuilder Create() =>
        SendInvoiceOnlineSessionRequestBuilderImpl.Create();
}
