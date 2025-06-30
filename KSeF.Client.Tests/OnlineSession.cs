using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions;
using System.Text;

namespace KSeF.Client.Tests;

public class OnlineSessionScenarioFixture
{
    public string? ReferenceNumber { get; set; }
    public string? InvoiceReferenceNumber { get; set; }
    public string? UpoDocument { get; set; }
    public string? UpoSession { get; set; }
    public string? AccessToken { get; set; }
    public string? UpoReferenceNumber { get; set; }

    public string? KsefNumber { get; set; }
}

[CollectionDefinition("OnlineSessionScenario")]
public class OnlineSessionScenarioCollection : ICollectionFixture<OnlineSessionScenarioFixture> { }

[Collection("OnlineSessionScenario")]
public class OnlineSession : TestBase
{
    private readonly OnlineSessionScenarioFixture _fixture;

    public OnlineSession(OnlineSessionScenarioFixture fixture)
    {
        _fixture = fixture;
        _fixture.AccessToken = AccessToken;
    }


    [Fact]
    public async Task OnlineSession_E2E_WorksCorrectly()
    {
        var cryptographyService = new CryptographyService(kSeFClient, restClient) as ICryptographyService;
        var encryptionData = cryptographyService.GetEncryptionData();
        // Step 1: Open session
        await Step1_OpenOnlineSession_ReturnsReference(encryptionData);
        Thread.Sleep(sleepTime); 
        // Step 2: Send invoice
        await Step2_SendInvoiceOnlineSessionAsync_ReturnsReferenceNumber(encryptionData, cryptographyService);
        Thread.Sleep(1000);
        // Step 3: Check status        
        await Step3_GetOnlineSessionStatusByAsync_ReturnsStatus();        
        Thread.Sleep(sleepTime);
        // Step 4: Close session
        await Step4_CloseOnlineSessionAsync_ClosesSessionSuccessfully();
        Thread.Sleep(2000);
        // Step 5: Get documents
        await Step5_GetOnlineSessionDocumentsAync_ReturnsDocuments();
        // Step 6: Get status after  close
        await Step6_GetOnlineSessionStatusByAsync_ReturnsStatus();
        // Step 7: Get UPO
        await Step7_GetOnlineSessionInvoiceUpoAsync_ReturnsUpo();
        // Step 8: Get session UPO
        await Step8_GetOnlineSessionUpoAsync_ReturnsSessionUpo();
    }

    
    private async Task Step1_OpenOnlineSession_ReturnsReference(EncryptionData encryptionData)
    {
        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
       .Create()
       .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
       .WithEncryption(
           encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
           initializationVector: encryptionData.EncryptionInfo.InitializationVector)
       .Build();

        var openOnlineSessionResponse = await kSeFClient.OpenOnlineSessionAsync(openOnlineSessionRequest, _fixture.AccessToken);

        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);
        _fixture.ReferenceNumber = openOnlineSessionResponse.ReferenceNumber;
    }
        
    private async Task Step2_SendInvoiceOnlineSessionAsync_ReturnsReferenceNumber(EncryptionData encryptionData , ICryptographyService cryptographyService)
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        
        //numer faktury unikalny rosnący
        //nip podmienić w fakturze
        var path = Path.Combine(AppContext.BaseDirectory, "invoices", "faktura-online.xml");
        var xml = File.ReadAllText(path, Encoding.UTF8);
        xml= xml.Replace("{{TEST_NIP}}",NIP);
        xml = xml.Replace("{{SEED_TEST_NIP_MONTH_YEAR}}",$"{Guid.NewGuid().ToString()}");
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var invoice = memoryStream.ToArray();

        var encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
        var invoiceMetadata = cryptographyService.GetMetaData(invoice);
        var encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        var sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithDocumentHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        var sendInvoiceResponse = await kSeFClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, _fixture.ReferenceNumber, _fixture.AccessToken);
                
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
        _fixture.InvoiceReferenceNumber = sendInvoiceResponse.ReferenceNumber;
    }

    private async Task Step3_GetOnlineSessionStatusByAsync_ReturnsStatus()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var statusResponse = await kSeFClient.GetSessionStatusAsync(_fixture.ReferenceNumber, _fixture.AccessToken);

        Assert.NotNull(statusResponse);

        //if ok GetSessionInvoicesAsync
        // if failed GetSessionFailedInvoicesAsync
        Assert.True(statusResponse.SuccessfulInvoiceCount is not null);
        Assert.True(statusResponse.SuccessfulInvoiceCount == 1);
        Assert.True(statusResponse.FailedInvoiceCount is null);
        Assert.Null(statusResponse.Upo);
        //sesja otwarta
        Assert.True(statusResponse.Status.Code == 100);
    }

    private async Task Step7_GetOnlineSessionInvoiceUpoAsync_ReturnsUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var upoResponse = await kSeFClient.GetSessionInvoiceUpoByKsefNumberAsync(_fixture.ReferenceNumber, _fixture.KsefNumber ,_fixture.AccessToken, CancellationToken.None);

        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoDocument = upoResponse;
    }

    private async Task Step4_CloseOnlineSessionAsync_ClosesSessionSuccessfully()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber)); // Wymuś wykonie kroku 1

        await kSeFClient.CloseOnlineSessionAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
    }


    private async Task Step6_GetOnlineSessionStatusByAsync_ReturnsStatus()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var statusResponse = await kSeFClient.GetSessionStatusAsync(_fixture.ReferenceNumber, _fixture.AccessToken);

        Assert.NotNull(statusResponse);
        _fixture.UpoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;
        Assert.True(statusResponse.Status.Code == 200);
    }

    private async Task Step5_GetOnlineSessionDocumentsAync_ReturnsDocuments()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var documents = await kSeFClient.GetSessionInvoicesAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
        Assert.NotNull(documents);
        Assert.NotEmpty(documents.Invoices);
        Assert.Single(documents.Invoices);

        _fixture.KsefNumber = documents.Invoices.First().KsefNumber;
    }
    private async Task Step8_GetOnlineSessionUpoAsync_ReturnsSessionUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var upoResponse = await kSeFClient.GetSessionUpoAsync(_fixture.ReferenceNumber, _fixture.UpoReferenceNumber, _fixture.AccessToken, CancellationToken.None);
        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoSession = upoResponse;
    }

}
