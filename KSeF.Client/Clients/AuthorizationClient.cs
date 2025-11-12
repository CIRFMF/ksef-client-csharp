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
    public Task<AuthenticationChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync<AuthenticationChallengeResponse>(Routes.Authorization.Challenge, HttpMethod.Post, cancellationToken);

    public Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedXML);

        string endpoint = Routes.Authorization.XadesSignature + $"?verifyCertificateChain={verifyCertificateChain.ToString().ToLower()}";
        string path = _routeBuilder.Build(endpoint);

        return _restClient.SendAsync<SignatureResponse, string>(HttpMethod.Post, path, signedXML, null, RestClient.XmlContentType, cancellationToken);
    }

    public Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthenticationKsefTokenRequest requestPayload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        return ExecuteAsync<SignatureResponse, AuthenticationKsefTokenRequest>(Routes.Authorization.KsefToken, requestPayload, cancellationToken);
    }

    public Task<AuthStatus> GetAuthStatusAsync(string authOperationReferenceNumber, string authenticationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationToken);
        string endpoint = Routes.Authorization.Status(Uri.EscapeDataString(authOperationReferenceNumber));
        return _restClient.SendAsync<AuthStatus, string>(HttpMethod.Get,
            _routeBuilder.Build(endpoint),
            default,
            authenticationToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }

    public Task<AuthenticationOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationToken);
        return _restClient.SendAsync<AuthenticationOperationStatusResponse, string>(HttpMethod.Post,
            _routeBuilder.Build(Routes.Authorization.Token.Redeem),
            default,
            authenticationToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }

    public Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return _restClient.SendAsync<RefreshTokenResponse, string>(HttpMethod.Post,
            _routeBuilder.Build(Routes.Authorization.Token.Refresh),
            default,
            refreshToken,
            RestClient.DefaultContentType,
            cancellationToken);
    }
}
