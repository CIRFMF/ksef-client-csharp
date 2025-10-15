using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Peppol;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using KSeF.Client.Http;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Permissions.Authorizations;

namespace KSeF.Client.Clients;

public partial class KSeFClient : IKSeFClient
{
    private readonly IRestClient restClient;

    public KSeFClient(IRestClient restClient) => this.restClient = restClient;

    /// <inheritdoc />
    public async Task<AuthenticationListResponse> GetActiveSessions(string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/auth/sessions");

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"?pageSize={pageSize.Value}");
        }

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<AuthenticationListResponse, object>(HttpMethod.Get,
                                                                            url,
                                                                            default,
                                                                            accessToken,
                                                                            RestClient.DefaultContentType,
                                                                            cancellationToken,
                                                                         !string.IsNullOrEmpty(continuationToken) ?
                                                                          new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                              : null).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task RevokeCurrentSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        await restClient.SendAsync(HttpMethod.Delete,
                                   "/api/v2/auth/sessions/current",
                                   token,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await restClient.SendAsync(HttpMethod.Delete,
                                   $"/api/v2/auth/sessions/{Uri.EscapeDataString(sessionReferenceNumber)}",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default)
    {
        return await restClient.SendAsync<AuthChallengeResponse, string>(HttpMethod.Post,
                                                                                       "/api/v2/auth/challenge",
                                                                                       default,
                                                                                       default,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedXML);

        string url = $"/api/v2/auth/xades-signature?verifyCertificateChain={verifyCertificateChain.ToString().ToLower()}";

        return await restClient.SendAsync<SignatureResponse, string>(HttpMethod.Post,
                                                                     url,
                                                                     signedXML,
                                                                     default,
                                                                     RestClient.XmlContentType,
                                                                     cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthKsefTokenRequest requestPayload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);

        return await restClient.SendAsync<SignatureResponse, AuthKsefTokenRequest>(HttpMethod.Post,
                                                                     "/api/v2/auth/ksef-token",
                                                                     requestPayload,
                                                                     default,
                                                                     RestClient.DefaultContentType,
                                                                     cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthStatus> GetAuthStatusAsync(string authOperationReferenceNumber, string authenticationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationToken);

        return await restClient.SendAsync<AuthStatus, string>(HttpMethod.Get,
                                                                   $"/api/v2/auth/{Uri.EscapeDataString(authOperationReferenceNumber)}",
                                                                   default,
                                                                   authenticationToken,
                                                                   RestClient.DefaultContentType,
                                                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationToken);

        return await restClient.SendAsync<AuthOperationStatusResponse, string>(HttpMethod.Post,
                                                                               $"/api/v2/auth/token/redeem",
                                                                               default,
                                                                               authenticationToken,
                                                                               RestClient.DefaultContentType,
                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return await restClient.SendAsync<RefreshTokenResponse, string>(HttpMethod.Post,
                                                                        $"/api/v2/auth/token/refresh",
                                                                        default,
                                                                        refreshToken,
                                                                        RestClient.DefaultContentType,
                                                                        cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OpenOnlineSessionResponse, OpenOnlineSessionRequest>(HttpMethod.Post,
                                                                                               "/api/v2/sessions/online",
                                                                                               requestPayload,
                                                                                               accessToken,
                                                                                               RestClient.DefaultContentType,
                                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<SendInvoiceResponse, SendInvoiceRequest>(HttpMethod.Post,
                                                                                    $"/api/v2/sessions/online/{Uri.EscapeDataString(sessionReferenceNumber)}/invoices",
                                                                                    requestPayload,
                                                                                    accessToken,
                                                                                    RestClient.DefaultContentType,
                                                                                    cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await restClient.SendAsync(HttpMethod.Post,
                                   $"/api/v2/sessions/online/{sessionReferenceNumber}/close",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OpenBatchSessionResponse> OpenBatchSessionAsync(OpenBatchSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OpenBatchSessionResponse, OpenBatchSessionRequest>(HttpMethod.Post, "/api/v2/sessions/batch", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseBatchSessionAsync(string batchSessionReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchSessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await restClient.SendAsync<object>(HttpMethod.Post,
                                           $"/api/v2/sessions/batch/{Uri.EscapeDataString(batchSessionReferenceNumber)}/close",
                                           default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async Task<SessionsListResponse> GetSessionsAsync(SessionType sessionType, string accessToken, int? pageSize, string continuationToken, SessionsFilter sessionsFilter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append($"/api/v2/sessions?sessionType={sessionType}");

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"&pageSize={pageSize.Value}");
        }

        if (sessionsFilter != null)
        {
            if (!string.IsNullOrEmpty(sessionsFilter.ReferenceNumber))
            {
                urlBuilder.Append($"&referenceNumber={Uri.EscapeDataString(sessionsFilter.ReferenceNumber)}");
            }
            if (sessionsFilter.DateCreatedFrom.HasValue)
            {
                urlBuilder.Append($"&dateCreatedFrom={Uri.EscapeDataString(sessionsFilter.DateCreatedFrom.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.DateCreatedTo.HasValue)
            {
                urlBuilder.Append($"&dateCreatedTo={Uri.EscapeDataString(sessionsFilter.DateCreatedTo.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.DateClosedFrom.HasValue)
            {
                urlBuilder.Append($"&dateClosedFrom={Uri.EscapeDataString(sessionsFilter.DateClosedFrom.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.DateClosedTo.HasValue)
            {
                urlBuilder.Append($"&dateClosedTo={Uri.EscapeDataString(sessionsFilter.DateClosedTo.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.DateModifiedFrom.HasValue)
            {
                urlBuilder.Append($"&dateModifiedFrom={Uri.EscapeDataString(sessionsFilter.DateModifiedFrom.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.DateModifiedTo.HasValue)
            {
                urlBuilder.Append($"&dateModifiedTo={Uri.EscapeDataString(sessionsFilter.DateModifiedTo.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
            }
            if (sessionsFilter.Statuses != null && sessionsFilter.Statuses.Any())
            {
                urlBuilder.Append($"&statuses={Uri.EscapeDataString(string.Join(",", sessionsFilter.Statuses))}");
            }
        }

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<SessionsListResponse, object>(HttpMethod.Get,
                                                                            url,
                                                                            default,
                                                                            accessToken,
                                                                            RestClient.DefaultContentType,
                                                                            cancellationToken,
                                                                            !string.IsNullOrEmpty(continuationToken) ?
                                                                             new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                              : null).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<SessionStatusResponse> GetSessionStatusAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<SessionStatusResponse, object>(HttpMethod.Get,
                                                                              $"/api/v2/sessions/{Uri.EscapeDataString(sessionReferenceNumber)}",
                                                                              default,
                                                                              accessToken,
                                                                              RestClient.DefaultContentType,
                                                                              cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/invoices");

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"?pageSize={pageSize.Value}");
        }

        var url = urlBuilder.ToString();


        return await restClient.SendAsync<SessionInvoicesResponse, object>(HttpMethod.Get,
                                                                                       url,
                                                                                       default,
                                                                                       accessToken,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken,
                                                                                       !string.IsNullOrEmpty(continuationToken) ?
                                                                                        new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                                        : null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInvoice> GetSessionInvoiceAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/invoices/");
        urlBuilder.Append(Uri.EscapeDataString(invoiceReferenceNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<SessionInvoice, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionFailedInvoicesResponse> GetSessionFailedInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/invoices/failed");

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"?pageSize={pageSize.Value}");
        }

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<SessionFailedInvoicesResponse, object>(HttpMethod.Get,
                                                                            url,
                                                                            default,
                                                                            accessToken,
                                                                            RestClient.DefaultContentType,
                                                                            cancellationToken,
                                                                            !string.IsNullOrEmpty(continuationToken) ?
                                                                             new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                                                                              : null).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<string> GetSessionInvoiceUpoByKsefNumberAsync(string sessionReferenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(ksefNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/invoices/ksef/");
        urlBuilder.Append(Uri.EscapeDataString(ksefNumber));
        urlBuilder.Append("/upo");

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<string> GetSessionInvoiceUpoByReferenceNumberAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/invoices/");
        urlBuilder.Append(Uri.EscapeDataString(invoiceReferenceNumber));
        urlBuilder.Append("/upo");

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetSessionUpoAsync(string sessionReferenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(upoReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(sessionReferenceNumber));
        urlBuilder.Append("/upo/");
        urlBuilder.Append(Uri.EscapeDataString(upoReferenceNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetInvoiceAsync(string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ksefNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/invoices/ksef/");
        urlBuilder.Append(Uri.EscapeDataString(ksefNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.XmlContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedInvoiceResponse> QueryInvoiceMetadataAsync(InvoiceQueryFilters requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/invoices/query/metadata");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedInvoiceResponse, InvoiceQueryFilters>(HttpMethod.Post,
                                                                    urlBuilder.ToString(),
                                                                    requestPayload,
                                                                    accessToken,
                                                                    RestClient.DefaultContentType,
                                                                    cancellationToken)
                                                                    .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/permissions/operations/");
        urlBuilder.Append(Uri.EscapeDataString(operationReferenceNumber));

        return await restClient.SendAsync<PermissionsOperationStatusResponse, string>(HttpMethod.Get,
                                                                                      urlBuilder.ToString(),
                                                                                      default,
                                                                                      accessToken,
                                                                                      RestClient.DefaultContentType,
                                                                                      cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> RevokeCommonPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, string>(HttpMethod.Delete,
                                                             $"/api/v2/permissions/common/grants/{Uri.EscapeDataString(permissionId)}",
                                                             default,
                                                             accessToken,
                                                             RestClient.DefaultContentType,
                                                             cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> RevokeAuthorizationsPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, string>(HttpMethod.Delete,
                                                             $"/api/v2/permissions/authorizations/grants/{Uri.EscapeDataString(permissionId)}",
                                                             default,
                                                             accessToken,
                                                             RestClient.DefaultContentType,
                                                             cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PermissionsAttachmentAllowedResponse> GetAttachmentPermissionStatusAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<PermissionsAttachmentAllowedResponse, string>(HttpMethod.Get,
                                                             "/api/v2/permissions/attachments/status",
                                                             default,
                                                             accessToken,
                                                             RestClient.DefaultContentType,
                                                             cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedPermissionsResponse<PersonalPermission>> SearchGrantedPersonalPermissionsAsync(
        PersonalPermissionsQueryRequest requestPayload,
              string accessToken,
              int? pageOffset = null,
              int? pageSize = null,
              CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/personal/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedPermissionsResponse<PersonalPermission>, PersonalPermissionsQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(
              PersonPermissionsQueryRequest requestPayload,
              string accessToken,
              int? pageOffset = null,
              int? pageSize = null,
              CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/persons/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedPermissionsResponse<PersonPermission>, PersonPermissionsQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedPermissionsResponse<SubunitPermission>> SearchSubunitAdminPermissionsAsync(
               Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest request,
               string accessToken,
               int? pageOffset = null,
               int? pageSize = null,
               CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/subunits/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedPermissionsResponse<SubunitPermission>, Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            request,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedRolesResponse<EntityRole>> SearchEntityInvoiceRolesAsync(
             string accessToken,
             int? pageOffset = null,
             int? pageSize = null,
             CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/entities/roles");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedRolesResponse<EntityRole>, object>(
            HttpMethod.Get,
            urlBuilder.ToString(),
            default,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedRolesResponse<SubordinateEntityRole>> SearchSubordinateEntityInvoiceRolesAsync(
           Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest request,
           string accessToken,
           int? pageOffset,
           int? pageSize,
           CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/subordinate-entities/roles");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedRolesResponse<SubordinateEntityRole>, Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            request,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchEntityAuthorizationGrantsAsync(
                   EntityAuthorizationsQueryRequest requestPayload,
                   string accessToken,
                   int? pageOffset,
                   int? pageSize,
                   CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/authorizations/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedAuthorizationsResponse<AuthorizationGrant>, EntityAuthorizationsQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedPermissionsResponse<EuEntityPermission>> SearchGrantedEuEntityPermissionsAsync(
           EuEntityPermissionsQueryRequest request,
           string accessToken,
           int? pageOffset = null,
           int? pageSize = null,
           CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/eu-entities/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedPermissionsResponse<EuEntityPermission>, EuEntityPermissionsQueryRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            request,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionPersonAsync(GrantPermissionsPersonRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsPersonRequest>(HttpMethod.Post,
                                                                                      "/api/v2/permissions/persons/grants",
                                                                                      requestPayload,
                                                                                      accessToken,
                                                                                      RestClient.DefaultContentType,
                                                                                      cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionEntityAsync(GrantPermissionsEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse,
            GrantPermissionsEntityRequest>(HttpMethod.Post,
                                           "/api/v2/permissions/entities/grants",
                                           requestPayload,
                                           accessToken,
                                           RestClient.DefaultContentType,
                                           cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsAuthorizationPermissionAsync(GrantAuthorizationPermissionsRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, GrantAuthorizationPermissionsRequest>(HttpMethod.Post,
                                 "/api/v2/permissions/authorizations/grants",
                                 requestPayload,
                                 accessToken,
                                 RestClient.DefaultContentType,
                                 cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionIndirectEntityAsync(
        GrantPermissionsIndirectEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsIndirectEntityRequest>(HttpMethod.Post,
                                                                                       "/api/v2/permissions/indirect/grants",
                                                                                       requestPayload,
                                                                                       accessToken,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken).ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionSubUnitAsync(
        Core.Models.Permissions.SubUnit.GrantPermissionsSubUnitRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse,
            Core.Models.Permissions.SubUnit.GrantPermissionsSubUnitRequest>(HttpMethod.Post,
                                                                                 "/api/v2/permissions/subunits/grants",
                                                                                 requestPayload,
                                                                                 accessToken,
                                                                                 RestClient.DefaultContentType,
                                                                                 cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionEUEntityAsync(
       GrantPermissionsRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsRequest>(
            HttpMethod.Post, "/api/v2/permissions/eu-entities/administration/grants", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync(
        GrantPermissionsEUEntitRepresentativeRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsEUEntitRepresentativeRequest>(
            HttpMethod.Post, "/api/v2/permissions/eu-entities/grants", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateLimitResponse> GetCertificateLimitsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<CertificateLimitResponse, object>(HttpMethod.Get, "/api/v2/certificates/limits", default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<CertificateEnrollmentsInfoResponse, object>(HttpMethod.Get,
                                                                                      "/api/v2/certificates/enrollments/data",
                                                                                      default,
                                                                                      accessToken,
                                                                                      RestClient.DefaultContentType,
                                                                                      cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(SendCertificateEnrollmentRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<CertificateEnrollmentResponse, SendCertificateEnrollmentRequest>(HttpMethod.Post,
                                                                                                           "/api/v2/certificates/enrollments",
                                                                                                           requestPayload,
                                                                                                           accessToken,
                                                                                                           RestClient.DefaultContentType,
                                                                                                           cancellationToken)
                                                                                                         .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string enrollmentReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enrollmentReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<CertificateEnrollmentStatusResponse, string>(HttpMethod.Get,
                                                                                       $"/api/v2/certificates/enrollments/{Uri.EscapeDataString(enrollmentReferenceNumber)}",
                                                                                       default, accessToken,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<CertificateListResponse, CertificateListRequest>(HttpMethod.Post,
                                                                                           $"/api/v2/certificates/retrieve",
                                                                                           requestPayload, accessToken,
                                                                                           RestClient.DefaultContentType,
                                                                                           cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeCertificateAsync(CertificateRevokeRequest requestPayload, string serialNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        await restClient.SendAsync(HttpMethod.Post,
                                   $"/api/v2/certificates/{Uri.EscapeDataString(serialNumber)}/revoke",
                                   requestPayload,
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateMetadataListResponse> GetCertificateMetadataListAsync(
       string accessToken,
       CertificateMetadataListRequest requestPayload = null,
       int? pageSize = null,
       int? pageOffset = null,
       CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/certificates/query");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<CertificateMetadataListResponse, CertificateMetadataListRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<KsefTokenResponse> GenerateKsefTokenAsync(KsefTokenRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<KsefTokenResponse, KsefTokenRequest>(HttpMethod.Post,
                                                                               "/api/v2/tokens",
                                                                               requestPayload,
                                                                               accessToken,
                                                                               RestClient.DefaultContentType,
                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QueryKsefTokensResponse> QueryKsefTokensAsync(
    string accessToken,
    ICollection<AuthenticationKsefTokenStatus> statuses = null,
    string authorIdentifier = null,
    Core.Models.Token.ContextIdentifierType? authorIdentifierType = null,
    string description = null,
    string continuationToken = null,
    int? pageSize = 10,
    CancellationToken cancellationToken = default)
    {
        var urlBuilder = new StringBuilder("/api/v2/tokens?");

        if (statuses != null && statuses.Any())
        {
            foreach (var s in statuses)
            {
                urlBuilder.Append($"status={Uri.EscapeDataString(s.ToString())}&");
            }
        }

        if (!string.IsNullOrWhiteSpace(authorIdentifier))
        {
            urlBuilder.Append($"authorIdentifier={Uri.EscapeDataString(authorIdentifier)}&");
        }

        if (authorIdentifierType.HasValue)
        {
            urlBuilder.Append($"authorIdentifierType={authorIdentifierType.Value}&");
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            urlBuilder.Append($"description={Uri.EscapeDataString(description)}&");
        }

        Pagination(null, pageSize, urlBuilder);

        return await restClient.SendAsync<QueryKsefTokensResponse, string>(
            HttpMethod.Get,
            urlBuilder.ToString().TrimEnd('&'),
            default,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken,
            !string.IsNullOrWhiteSpace(continuationToken)
                            ? new Dictionary<string, string> { { "x-continuation-token", Regex.Unescape(continuationToken) } }
                            : null
                    ).ConfigureAwait(false);
    }



    /// <inheritdoc />
    public async Task<AuthenticationKsefToken> GetKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await restClient.SendAsync<AuthenticationKsefToken, string>(HttpMethod.Get,
                                                                            $"/api/v2/tokens/{Uri.EscapeDataString(tokenReferenceNumber)}",
                                                                            default,
                                                                            accessToken,
                                                                            RestClient.DefaultContentType,
                                                                            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await restClient.SendAsync(HttpMethod.Delete,
                                   $"/api/v2/tokens/{Uri.EscapeDataString(tokenReferenceNumber)}",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QueryPeppolProvidersResponse> QueryPeppolProvidersAsync(
     string accessToken,
     int? pageOffset = null,
     int? pageSize = null,
     CancellationToken cancellationToken = default)
    {
        var urlBuilder = new StringBuilder("/api/v2/peppol/query?");

        if (pageOffset.HasValue)
            urlBuilder.Append($"pageOffset={pageOffset.Value}&");

        if (pageSize.HasValue)
            urlBuilder.Append($"pageSize={pageSize.Value}&");

        var url = urlBuilder.ToString().TrimEnd('&').TrimEnd('?');

        return await restClient.SendAsync<QueryPeppolProvidersResponse, string>(
            HttpMethod.Get,
            url,
            default,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default)
    {
        if (parts == null || parts.Count == 0)
            throw new ArgumentException("Brak plików do wysłania.", nameof(parts));

        await SendPartsInternalAsync(
            openBatchSessionResponse.PartUploadRequests,
            parts,
            (info) => new ByteArrayContent(info.Data),
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendBatchPartsWithStreamAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartStreamSendingInfo> parts, CancellationToken cancellationToken = default)
    {
        if (parts == null || parts.Count == 0)
            throw new ArgumentException("Brak plików do wysłania.", nameof(parts));

        await SendPartsInternalAsync(
            openBatchSessionResponse.PartUploadRequests,
            parts,
            (info) => new StreamContent(info.DataStream),
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ExportInvoicesResponse> ExportInvoicesAsync(
    InvoiceExportRequest requestPayload,
    string accessToken,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var urlBuilder = new StringBuilder("/api/v2/invoices/exports");

        return await restClient.SendAsync<ExportInvoicesResponse, InvoiceExportRequest>(
            HttpMethod.Post,
            urlBuilder.ToString(),
            requestPayload,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async Task<InvoiceExportStatusResponse> GetInvoiceExportStatusAsync(
    string operationReferenceNumber,
    string accessToken,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationReferenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var url = $"/api/v2/invoices/exports/{Uri.EscapeDataString(operationReferenceNumber)}";

        return await restClient.SendAsync<InvoiceExportStatusResponse, object>(
            HttpMethod.Get,
            url,
            default,
            accessToken,
            RestClient.DefaultContentType,
            cancellationToken
        ).ConfigureAwait(false);
    }


    private static async Task SendPartsInternalAsync<TInfo>(
        ICollection<PackagePartSignatureInitResponseType> parts,
        ICollection<TInfo> batchPartSendingInfos,
        Func<TInfo, HttpContent> contentFactory,
        CancellationToken cancellationToken)
        where TInfo : class, IBatchOriginalNumber
    {
        if (parts == null)
            throw new InvalidOperationException("Brak informacji o partach do wysłania.");

        using var httpClient = new HttpClient();
        var errors = new List<string>();

        foreach (var part in parts)
        {
            var fileInfo = batchPartSendingInfos.FirstOrDefault(x =>
                x.OrdinalNumber == part.OrdinalNumber);

            if (fileInfo == null)
            {
                errors.Add($"Brak danych dla partu {part.OrdinalNumber}.");
                continue;
            }

            using var content = contentFactory(fileInfo);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            if (part.Headers != null)
            {
                foreach (var header in part.Headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }
            }

            if (string.IsNullOrWhiteSpace(part.Method))
            {
                errors.Add($"Brak metody HTTP dla partu {part.OrdinalNumber}.");
                continue;
            }

            var method = new HttpMethod(part.Method.ToUpperInvariant());
            using var request = new HttpRequestMessage(method, part.Url)
            {
                Content = content
            };

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                errors.Add($"Błąd wysyłki partu {part.OrdinalNumber}: {response.StatusCode} {error}");
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException("Wystąpiły błędy podczas wysyłania partów wsadowych.", errors.Select(e => new Exception(e)));
        }
    }

    private static void Pagination(int? pageOffset, int? pageSize, StringBuilder urlBuilder)
    {
        var hasQuery = false;
        if (pageSize.HasValue && pageSize > 0)
        {
            urlBuilder.Append(hasQuery ? "&" : "?");
            urlBuilder.Append("pageSize=").Append(Uri.EscapeDataString(pageSize.ToString()));
            hasQuery = true;
        }
        if (pageOffset.HasValue && pageOffset > 0)
        {
            urlBuilder.Append(hasQuery ? "&" : "?");
            urlBuilder.Append("pageOffset=").Append(Uri.EscapeDataString(pageOffset.ToString()));
            hasQuery = true;
        }
    }
}