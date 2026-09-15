#nullable enable
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Authorization;

/// <summary>
/// Testy E2E metod <see cref="AuthCoordinator"/>: AuthAsync (XAdES), AuthKsefTokenAsync i RefreshAccessTokenAsync.
/// </summary>
public class AuthCoordinatorE2ETests : TestBase
{
    private IAuthCoordinator CreateCoordinator() => new AuthCoordinator(AuthorizationClient);

    /// <summary>
    /// Uwierzytelnienie XAdES: challenge, podpis certyfikatem (RSA lub EC), wysłanie, polling, para tokenów.
    /// </summary>
    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    [InlineData(EncryptionMethodEnum.ECDsa)]
    public async Task AuthAsync_WithSigningCertificate_ReturnsAccessAndRefreshToken(EncryptionMethodEnum signingKeyAlgorithm)
    {
        // Arrange
        string nip = MiscellaneousUtils.GetRandomNip();
        X509Certificate2 certificate =
            CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R", signingKeyAlgorithm);
        IAuthCoordinator coordinator = CreateCoordinator();

        // Act
        AuthenticationOperationStatusResponse result = await coordinator.AuthAsync(
            AuthenticationTokenContextIdentifierType.Nip,
            nip,
            AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
            xmlSigner: xml => Task.FromResult(SignatureService.Sign(xml, certificate)),
            authorizationPolicy: null,
            cancellationToken: CancellationToken);

        // Assert
        Assert.NotNull(result.AccessToken?.Token);
        Assert.NotNull(result.RefreshToken?.Token);
    }

    /// <summary>
    /// Logowanie tokenem KSeF przez koordynator.
    /// </summary>
    [Fact]
    public async Task AuthKsefTokenAsync_WithActiveToken_ReturnsAccessAndRefreshToken()
    {
        // Arrange
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);

        KsefTokenPermissionType[] permissions =
        [
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        ];

        KsefTokenResponse ksefToken = await KsefClient.GenerateKsefTokenAsync(
            new KsefTokenRequest { Description = "Wystawianie i przeglądanie faktur", Permissions = permissions },
            ownerAuth.AccessToken.Token,
            CancellationToken);

        // Token KSeF nie jest aktywny natychmiast po wygenerowaniu, odpytujemy aż do statusu Active.
        AuthenticationKsefToken activeToken = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.GetKsefTokenAsync(ksefToken.ReferenceNumber, ownerAuth.AccessToken.Token, CancellationToken),
            condition: token => token is { Status: AuthenticationKsefTokenStatus.Active },
            description: "Oczekiwanie na aktywację tokenu KSeF",
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60);

        IAuthCoordinator coordinator = CreateCoordinator();

        // Act
        AuthenticationOperationStatusResponse result = await coordinator.AuthKsefTokenAsync(
            AuthenticationTokenContextIdentifierType.Nip,
            activeToken.ContextIdentifier.Value,
            ksefToken.Token,
            CryptographyService,
            EncryptionMethodEnum.Rsa,
            authorizationPolicy: null,
            cancellationToken: CancellationToken);

        // Assert
        Assert.NotNull(result.AccessToken?.Token);
        Assert.NotNull(result.RefreshToken?.Token);
    }

    /// <summary>
    /// <see cref="IAuthCoordinator.RefreshAccessTokenAsync"/> wymienia refresh token na nowy access token.
    /// </summary>
    [Fact]
    public async Task RefreshAccessTokenAsync_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse auth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);
        IAuthCoordinator coordinator = CreateCoordinator();

        // Act
        TokenInfo refreshed = await coordinator.RefreshAccessTokenAsync(auth.RefreshToken.Token, CancellationToken);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
        Assert.NotEqual(auth.AccessToken.Token, refreshed.Token);
    }
}
