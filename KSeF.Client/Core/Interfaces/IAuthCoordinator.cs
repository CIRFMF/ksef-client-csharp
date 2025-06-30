using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;

namespace KSeFClient.Core.Interfaces;

public interface IAuthCoordinator
{
    /// <summary>
    /// Pobiera Access + Refresh tokeny na podstawie referenceNumber.
    /// </summary>
    Task<AuthOperationStatusResponse> AuthAsync(
        ContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        SubjectIdentifierTypeEnum identifierType,
        Func<string, Task<string>> xmlSigner,
        IpAddressPolicy ipAddressPolicy = default,
        CancellationToken ct = default,
        bool verifyCertificateChain = false);

    /// <summary>
    /// Odświeża AccessToken na podstawie poprzedniego RefreshToken.
    /// </summary>
    Task<TokenInfo> RefreshAccessTokenAsync(string refreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke’uje AccessToken.
    /// </summary>
    Task RevokeTokenAsync(string accessToken,
        CancellationToken ct = default);

    Task<AuthOperationStatusResponse> AuthKsefTokenAsync(
        ContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        string tokenKsef,
        ICryptographyService cryptographyService,
        IpAddressPolicy ipAddressPolicy = default,
        CancellationToken ct = default);
}
