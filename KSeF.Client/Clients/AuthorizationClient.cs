using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Http;

namespace KSeF.Client.Clients;

/// <summary>
/// Klient odpowiedzialny za operacje uwierzytelniania.
/// </summary>
public class AuthorizationClient(IRestClient restClient, IRouteBuilder routeBuilder)
    : ClientBase(restClient, routeBuilder), IAuthorizationClient
{
    /// <inheritdoc/>
    public Task<AuthenticationChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync<AuthenticationChallengeResponse>(Routes.Authorization.Challenge, HttpMethod.Post, cancellationToken);

    /// <inheritdoc/>
    public Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(signedXML);

        string endpoint = Routes.Authorization.XadesSignature + $"?verifyCertificateChain={verifyCertificateChain.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}";
        string path = routeBuilder.Build(endpoint);

        return restClient.SendAsync<SignatureResponse, string>(HttpMethod.Post, path, signedXML, null, RestClient.XmlContentType, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthenticationKsefTokenRequest requestPayload, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(requestPayload);
        return ExecuteAsync<SignatureResponse, AuthenticationKsefTokenRequest>(Routes.Authorization.KsefToken, requestPayload, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuthStatus> GetAuthStatusAsync(string authOperationReferenceNumber, string authenticationToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(authenticationToken);
        string endpoint = Routes.Authorization.Status(Uri.EscapeDataString(authOperationReferenceNumber));
        return restClient.SendAsync<AuthStatus, string>(HttpMethod.Get,
            routeBuilder.Build(endpoint),
            default,
            authenticationToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuthenticationOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(authenticationToken);
        return restClient.SendAsync<AuthenticationOperationStatusResponse, string>(HttpMethod.Post,
            routeBuilder.Build(Routes.Authorization.Token.Redeem),
            default,
            authenticationToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(refreshToken);
        return restClient.SendAsync<RefreshTokenResponse, string>(HttpMethod.Post,
            routeBuilder.Build(Routes.Authorization.Token.Refresh),
            default,
            refreshToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }
}
