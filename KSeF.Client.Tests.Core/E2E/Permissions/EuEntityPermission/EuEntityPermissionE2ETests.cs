using KSeF.Client.Api.Builders.EuEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuEntityPermissions;

[Collection("EuEntityPermissionE2EScenarioCollection")]
public class EuEntityPermissionE2ETests : TestBase
{
    private const string EuEntitySubjectName = "Sample Subject Name";
    private const string EuEntityDescription = "E2E EU Entity Permission Test";
    private const int OperationSuccessfulStatusCode = 200;

    private readonly EuEntityPermissionsQueryRequest EuEntityPermissionsQueryRequest =
            new EuEntityPermissionsQueryRequest { /* e.g. filtrowanie */ };
    private readonly EuEntityPermissionScenarioE2EFixture TestFixture;
    private string accessToken = string.Empty;

    public EuEntityPermissionE2ETests()
    {
        string nip = MiscellaneousUtils.GetRandomNip();
        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R");
        string fingerprint = GetFingerprintWithSeparators(certificate, "SHA256", "");
        TestFixture = new EuEntityPermissionScenarioE2EFixture();
        TestFixture.NipVatUe = MiscellaneousUtils.GetRandomNipVatEU(nip, "CZ");
        AuthenticationOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, nip).GetAwaiter().GetResult();
        accessToken = authOperationStatusResponse.AccessToken.Token;
        TestFixture.EuEntity.Value = fingerprint;
    }

    /// <summary>
    /// Nadaje uprawnienia dla podmiotu, weryfikuje ich nadanie, następnie odwołuje nadane uprawnienia i ponownie weryfikuje.
    /// </summary>
    [Fact]
    public async Task EuEntityGrantSearchRevokeSearch_E2E_ReturnsExpectedResults()
    {
        #region Nadaj uprawnienia jednostce EU
        // Arrange
        EuEntityContextIdentifier contextIdentifier = new EuEntityContextIdentifier
        {
            Type = EuEntityContextIdentifierType.NipVatUe,
            Value = TestFixture.NipVatUe
        };

        // Act
        OperationResponse operationResponse = await GrantPermissionForEuEntityAsync(contextIdentifier);
        TestFixture.GrantResponse = operationResponse;

        // Assert
        Assert.NotNull(TestFixture.GrantResponse);
        Assert.False(string.IsNullOrEmpty(TestFixture.GrantResponse.ReferenceNumber));
        #endregion

        #region Wyszukaj nadane uprawnienia
        // Act
        PagedPermissionsResponse<EuEntityPermission> grantedPermissionsPaged =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchPermissionsAsync(EuEntityPermissionsQueryRequest),
                result => result is not null && result.Permissions is { Count: > 0 },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);
        TestFixture.SearchResponse = grantedPermissionsPaged;

        // Assert
        Assert.NotNull(TestFixture.SearchResponse);
        Assert.NotEmpty(TestFixture.SearchResponse.Permissions);
        #endregion

        #region Odwołaj uprawnienia
        // Act
        await RevokePermissionsAsync();

        Assert.NotNull(TestFixture.RevokeStatusResults);
        Assert.NotEmpty(TestFixture.RevokeStatusResults);
        Assert.Equal(TestFixture.RevokeStatusResults.Count, TestFixture.SearchResponse.Permissions.Count);
        Assert.All(TestFixture.RevokeStatusResults, r =>
            Assert.True(r.Status.Code == OperationSuccessfulStatusCode,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );
        #endregion

        #region Sprawdź czy po odwołaniu uprawnienia już nie występują
        // Act
        PagedPermissionsResponse<EuEntityPermission> euEntityPermissionsWhenRevoked =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchPermissionsAsync(EuEntityPermissionsQueryRequest),
                result => result is not null && (result.Permissions is null || result.Permissions.Count == 0),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);
        TestFixture.SearchResponse = euEntityPermissionsWhenRevoked;

        // Assert
        Assert.NotNull(TestFixture.SearchResponse);
        Assert.Empty(TestFixture.SearchResponse.Permissions);
        #endregion
    }

    /// <summary>
    /// Tworzy żądanie nadania uprawnień jednostce UE oraz wysyła żądanie do KSeF API.
    /// </summary>
    /// <param name="contextIdentifier"></param>
    /// <returns>Numer referencyjny operacji</returns>
    private async Task<OperationResponse> GrantPermissionForEuEntityAsync(EuEntityContextIdentifier contextIdentifier)
    {
        GrantPermissionsEuEntityRequest grantPermissionsRequest = GrantEuEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(TestFixture.EuEntity)
            .WithSubjectName(EuEntitySubjectName)
            .WithContext(contextIdentifier)
            .WithDescription(EuEntityDescription)
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsPermissionEUEntityAsync(grantPermissionsRequest, accessToken, CancellationToken);

        return operationResponse;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane jednostce EU.
    /// </summary>
    /// <param name="expectAny"></param>
    /// <returns>Stronicowana lista wyszukanych uprawnień</returns>
    private async Task<PagedPermissionsResponse<EuEntityPermission>> SearchPermissionsAsync(EuEntityPermissionsQueryRequest euEntityPermissionsQueryRequest)
    {
        PagedPermissionsResponse<EuEntityPermission> response =
            await KsefClient
            .SearchGrantedEuEntityPermissionsAsync(
                euEntityPermissionsQueryRequest,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return response;
    }

    /// <summary>
    /// Wysyła żądanie odwołania uprawnień do KSeF API.
    /// </summary>
    private async Task RevokePermissionsAsync()
    {
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        foreach (EuEntityPermission permission in TestFixture.SearchResponse.Permissions)
        {
            OperationResponse operationResponse = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(operationResponse);
        }

        foreach (OperationResponse revokeResponse in revokeResponses)
        {
            PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.ReferenceNumber, accessToken),
                    s => s is not null && s.Status is not null && s.Status.Code == OperationSuccessfulStatusCode,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken);

            TestFixture.RevokeStatusResults.Add(status);
        }
    }

    /// <summary>
    /// Zwraca fingerprint certyfikatu.
    /// </summary>
    /// <param name="certificate">Certyfikat typu X509Certificate2</param>
    /// <param name="algorithmName">Algorytm certyfikatu</param>
    /// <param name="separator">Separator fingerprint</param>
    /// <returns></returns>
    private string GetFingerprintWithSeparators(X509Certificate2 certificate, string algorithmName = "SHA256", string separator = ":")
    {
        byte[] raw = certificate.RawData;
        using HashAlgorithm hash = algorithmName.ToUpperInvariant() switch
        {
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => HashAlgorithm.Create(algorithmName) ?? SHA256.Create()
        };
        byte[] digest = hash.ComputeHash(raw);
        return string.Join(separator, digest.Select(b => b.ToString("X2"))); // wielkie litery
    }
}