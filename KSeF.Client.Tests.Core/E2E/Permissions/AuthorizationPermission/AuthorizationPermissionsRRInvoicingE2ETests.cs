#nullable enable
using KSeF.Client.Api.Builders.AuthorizationEntityPermissions;
using KSeF.Client.Api.Builders.Online;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Tests.Utils;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermission;

public class AuthorizationPermissionsRRInvoicingE2ETests : TestBase
{
    private const string TemplateFileName = "invoice-template-fa-rr-1.xml";
    private const string GrantorDescriptionPrefix = "E2E-RR-Grantor";
    private const string AuthorizedDescriptionPrefix = "E2E-RR-Authorized";
    private const string PermissionDescriptionPrefix = "E2E-RRInvoicing";
    private const string AuthorizedSubjectFullName = "Podmiot Testowy RR";
    private const string PollingDescriptionGrantPermission = "Czekam na nadanie uprawnienia RRInvoicing";
    private const string PollingDescriptionProcessInvoice = "Czekam na przetworzenie faktury RR";
    private const string PollingDescriptionVisiblePermission = "Czekam na widoczność nadanego RRInvoicing";
    private const string PollingDescriptionRevokePermission = "Czekam na odebranie uprawnienia RRInvoicing";
    private const int ExpectedSuccessfulInvoiceCount = 1;
    private const int PageOffset = 0;
    private const int PageSize = 50;

    /// <summary>
    /// Weryfikacja poprawności nadawania uprawnienia RRInvoicing i wysyłania faktury FA-RR.
    /// Scenariusz:
    /// 1. Utworzenie dwóch podmiotów testowych (grantor i authorized)
    /// 2. Nadanie uprawnienia RRInvoicing przez grantor dla authorized
    /// 3. Otwarcie sesji online przez authorized z kodem systemu FA_RR
    /// 4. Wysłanie faktury FA-RR przez authorized w imieniu grantor
    /// 5. Weryfikacja przetworzenia faktury
    /// 6. Zamknięcie sesji i wyszukanie nadanego uprawnienia
    /// 7. Odebranie uprawnienia RRInvoicing
    /// 8. Usunięcie podmiotów testowych
    /// </summary>
    [Fact]
    public async Task RRInvoicingPermission_AllowsSendingFaRrInvoice()
    {
        // Arrange - Przygotowanie podmiotów testowych
        string grantorNip = MiscellaneousUtils.GetRandomNip();
        string authorizedNip = MiscellaneousUtils.GetRandomNip();

        await TestDataClient.CreateSubjectAsync(
            new SubjectCreateRequest { SubjectNip = grantorNip, Description = $"{GrantorDescriptionPrefix}-{grantorNip}" },
            CancellationToken);
        await TestDataClient.CreateSubjectAsync(
            new SubjectCreateRequest { SubjectNip = authorizedNip, Description = $"{AuthorizedDescriptionPrefix}-{authorizedNip}" },
            CancellationToken);

        // Arrange - Uwierzytelnienie jako podmiot nadający uprawnienie (grantor)
        AuthenticationOperationStatusResponse grantorAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, grantorNip);
        string grantorAccessToken = grantorAuth.AccessToken.Token;

        // Act - Nadanie uprawnienia RRInvoicing
        GrantPermissionsAuthorizationRequest grantRequest =
            GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new AuthorizationSubjectIdentifier
                {
                    Type = AuthorizationSubjectIdentifierType.Nip,
                    Value = authorizedNip
                })
                .WithPermission(AuthorizationPermissionType.RRInvoicing)
                .WithDescription($"{PermissionDescriptionPrefix}-{authorizedNip}")
                .WithSubjectDetails(new PermissionsAuthorizationSubjectDetails
                {
                    FullName = AuthorizedSubjectFullName
                })
                .Build();

        OperationResponse grantOperation =
            await KsefClient.GrantsAuthorizationPermissionAsync(grantRequest, grantorAccessToken, CancellationToken);

        // Assert - Weryfikacja nadania uprawnienia
        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOperation.ReferenceNumber, grantorAccessToken).ConfigureAwait(false),
                result => result.Status.Code == OperationStatusCodeResponse.Success,
                description: PollingDescriptionGrantPermission,
                cancellationToken: CancellationToken);

        Assert.NotNull(grantStatus);

        // Arrange - Uwierzytelnienie jako podmiot uprawniony (authorized)
        AuthenticationOperationStatusResponse authorizedAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, authorizedNip);
        string authorizedAccessToken = authorizedAuth.AccessToken.Token;

        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Act - Otwarcie sesji online z kodem systemu FA_RR
        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            authorizedAccessToken,
            SystemCode.FA_RR);

        // Assert - Weryfikacja otwarcia sesji
        Assert.NotNull(openSessionResponse);
        Assert.False(string.IsNullOrWhiteSpace(openSessionResponse.ReferenceNumber));

        // Act - Wysłanie faktury FA-RR
        string invoiceXml = PrepareInvoiceXml(TemplateFileName, grantorNip, authorizedNip);

        SendInvoiceResponse sendInvoiceResponse = await SendInvoiceFromXmlAsync(
            openSessionResponse.ReferenceNumber,
            authorizedAccessToken,
            encryptionData,
            invoiceXml);

        // Assert - Weryfikacja wysłania faktury
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));

        // Assert - Weryfikacja przetworzenia faktury
        SessionStatusResponse statusAfterSend = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetSessionStatusAsync(openSessionResponse.ReferenceNumber, authorizedAccessToken)
                .ConfigureAwait(false),
            result => result is not null && result.SuccessfulInvoiceCount is not null,
            description: PollingDescriptionProcessInvoice,
            cancellationToken: CancellationToken);

        Assert.NotNull(statusAfterSend);
        Assert.Equal(ExpectedSuccessfulInvoiceCount, statusAfterSend.SuccessfulInvoiceCount);
        Assert.True(statusAfterSend.FailedInvoiceCount is null);

        // Act - Zamknięcie sesji online
        await KsefClient.CloseOnlineSessionAsync(openSessionResponse.ReferenceNumber, authorizedAccessToken);

        // Arrange - Przygotowanie żądania wyszukania nadanych uprawnień
        EntityAuthorizationsQueryRequest searchRequest = new()
        {
            AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier
            {
                Type = EntityAuthorizationsAuthorizingEntityIdentifierType.Nip,
                Value = grantorNip
            },
            AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
            {
                Type = EntityAuthorizationsAuthorizedEntityIdentifierType.Nip,
                Value = authorizedNip
            },
            QueryType = QueryType.Granted,
            PermissionTypes = [InvoicePermissionType.RRInvoicing]
        };

        // Act - Wyszukanie nadanego uprawnienia RRInvoicing
        PagedAuthorizationsResponse<AuthorizationGrant> grants = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.SearchEntityAuthorizationGrantsAsync(
                searchRequest, grantorAccessToken, pageOffset: PageOffset, pageSize: PageSize, cancellationToken: CancellationToken)
                .ConfigureAwait(false),
            result => result.AuthorizationGrants is { Count: > 0 },
            description: PollingDescriptionVisiblePermission,
            cancellationToken: CancellationToken);

        // Assert - Weryfikacja znalezienia nadanego uprawnienia
        AuthorizationGrant? matching = grants.AuthorizationGrants.FirstOrDefault(g =>
            g.AuthorizationScope == AuthorizationPermissionType.RRInvoicing &&
            g.AuthorizedEntityIdentifier?.Value == authorizedNip);

        Assert.NotNull(matching);

        // Act - Odebranie uprawnienia RRInvoicing
        OperationResponse revokeOperation = await KsefClient.RevokeAuthorizationsPermissionAsync(
            matching!.Id,
            grantorAccessToken,
            CancellationToken);

        // Assert - Weryfikacja odebrania uprawnienia
        PermissionsOperationStatusResponse revokeStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.OperationsStatusAsync(revokeOperation.ReferenceNumber, grantorAccessToken)
                .ConfigureAwait(false),
            result => result.Status.Code == OperationStatusCodeResponse.Success,
            description: PollingDescriptionRevokePermission,
            cancellationToken: CancellationToken);

        Assert.NotNull(revokeStatus);

        // Cleanup - Usunięcie podmiotów testowych
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = authorizedNip }, CancellationToken);
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = grantorNip }, CancellationToken);
    }

    /// <summary>
    /// Przygotowuje XML faktury na podstawie szablonu z podmienionymi danymi NIP.
    /// </summary>
    /// <param name="templateName">Nazwa pliku szablonu faktury</param>
    /// <param name="supplierNip">NIP dostawcy (sprzedawcy)</param>
    /// <param name="buyerNip">NIP nabywcy (kupującego)</param>
    /// <returns>XML faktury z podmienionymi danymi</returns>
    private static string PrepareInvoiceXml(string templateName, string supplierNip, string buyerNip)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
        string xml = File.ReadAllText(path, Encoding.UTF8);

        xml = xml.Replace("#nip#", supplierNip);
        xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");
        xml = xml.Replace("#buyer_nip#", buyerNip);

        return xml;
    }

    /// <summary>
    /// Wysyła zaszyfrowaną fakturę w ramach sesji online.
    /// </summary>
    /// <param name="sessionReferenceNumber">Numer referencyjny sesji</param>
    /// <param name="accessToken">Token dostępu</param>
    /// <param name="encryptionData">Dane szyfrowania</param>
    /// <param name="xml">XML faktury do wysłania</param>
    /// <returns>Odpowiedź z numerem referencyjnym wysłanej faktury</returns>
    private async Task<SendInvoiceResponse> SendInvoiceFromXmlAsync(
        string sessionReferenceNumber,
        string accessToken,
        EncryptionData encryptionData,
        string xml)
    {
        using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));
        byte[] invoice = memoryStream.ToArray();

        byte[] encryptedInvoice = CryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
        FileMetadata invoiceMetadata = CryptographyService.GetMetaData(invoice);
        FileMetadata encryptedInvoiceMetadata = CryptographyService.GetMetaData(encryptedInvoice);

        SendInvoiceRequest sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        return await KsefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken)
            .ConfigureAwait(false);
    }
}
