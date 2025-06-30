using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.ProxyEntity;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeFClient.Core.Interfaces;
using KSeFClient.Core.Models;
using System.Text;



namespace KSeFClient.Http;

public partial class KSeFClient : IKSeFClient
{
    private readonly IRestClient restClient;

    public KSeFClient(IRestClient restClient) => this.restClient = restClient;

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
        ValidateParams(signedXML);

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
        ValidatePayload(requestPayload);

        return await restClient.SendAsync<SignatureResponse, AuthKsefTokenRequest>(HttpMethod.Post,
                                                                     "/api/v2/auth/ksef-token",
                                                                     requestPayload,
                                                                     default,
                                                                     RestClient.DefaultContentType,
                                                                     cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthStatus> GetAuthStatusAsync(string referenceNumber, string authenticationToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(authenticationToken);
        return await restClient.SendAsync<AuthStatus, string>(HttpMethod.Get,
                                                                   $"/api/v2/auth/{Uri.EscapeDataString(referenceNumber)}",
                                                                   default,
                                                                   authenticationToken,
                                                                   RestClient.DefaultContentType,
                                                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(authenticationToken);

        return await restClient.SendAsync<AuthOperationStatusResponse, string>(HttpMethod.Post,
                                                                               $"/api/v2/auth/token/redeem",//TODO
                                                                               default,
                                                                               authenticationToken,
                                                                               RestClient.DefaultContentType,
                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(refreshToken);

        return await restClient.SendAsync<RefreshTokenResponse, string>(HttpMethod.Post,
                                                                        $"/api/v2/auth/token/refresh",
                                                                        default,
                                                                        refreshToken,
                                                                        RestClient.DefaultContentType,
                                                                        cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken);

        await restClient.SendAsync(HttpMethod.Delete,
                                   $"/api/v2/auth/token",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ICollection<PemCertificateInfo>> GetPublicCertificates(CancellationToken cancellationToken)
    {
        return await restClient.SendAsync<ICollection<PemCertificateInfo>, string>(HttpMethod.Get,
                                                                      "/api/v2/security/public-key-certificates",
                                                                      default,
                                                                      default,
                                                                      RestClient.DefaultContentType,
                                                                      cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OpenOnlineSessionResponse, OpenOnlineSessionRequest>(HttpMethod.Post,
                                                                                               "/api/v2/sessions/online",
                                                                                               requestPayload,
                                                                                               accessToken,
                                                                                               RestClient.DefaultContentType,
                                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ValidateParams(referenceNumber);
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<SendInvoiceResponse, SendInvoiceRequest>(HttpMethod.Post,
                                                                                    $"/api/v2/sessions/online/{Uri.EscapeDataString(referenceNumber)}/invoices",
                                                                                    requestPayload,
                                                                                    accessToken,
                                                                                    RestClient.DefaultContentType,
                                                                                    cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseOnlineSessionAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ValidateParams(referenceNumber, accessToken);
        await restClient.SendAsync(HttpMethod.Post,
                                   $"/api/v2/sessions/online/{referenceNumber}/close",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OpenBatchSessionResponse> OpenBatchSessionAsync(OpenBatchSessionRequest requestPayload, string accessToken, CancellationToken cancellationToken)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OpenBatchSessionResponse, OpenBatchSessionRequest>(HttpMethod.Post, "/api/v2/sessions/batch", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseBatchSessionAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        ValidateParams(referenceNumber, accessToken);

        await restClient.SendAsync<object>(HttpMethod.Post,
                                           $"/api/v2/sessions/batch/{Uri.EscapeDataString(referenceNumber)}/close",
                                           default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionStatusResponse> GetSessionStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);

        return await restClient.SendAsync<SessionStatusResponse, object>(HttpMethod.Get,
                                                                              $"/api/v2/sessions/{Uri.EscapeDataString(referenceNumber)}",
                                                                              default,
                                                                              accessToken,
                                                                              RestClient.DefaultContentType,
                                                                              cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string referenceNumber, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/invoices");

        Pagination(pageOffset, pageSize, urlBuilder);
        var url = urlBuilder.ToString();


        return await restClient.SendAsync<SessionInvoicesResponse, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInvoice> GetSessionInvoiceAsync(string referenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, invoiceReferenceNumber, accessToken);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/invoices/");
        urlBuilder.Append(Uri.EscapeDataString(invoiceReferenceNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<SessionInvoice, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionInvoicesResponse> GetSessionFailedInvoicesAsync(string referenceNumber, string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/invoices/failed");

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
                                                                             new Dictionary<string, string> { { "x-continuation-token", continuationToken } }
                                                                              : null).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<string> GetSessionInvoiceUpoByKsefNumberAsync(string referenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, ksefNumber, accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/invoices/ksef/");
        urlBuilder.Append(Uri.EscapeDataString(ksefNumber));
        urlBuilder.Append("/upo");

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<string> GetSessionInvoiceUpoByReferenceNumberAsync(string referenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, invoiceReferenceNumber, accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/invoices/");
        urlBuilder.Append(Uri.EscapeDataString(invoiceReferenceNumber));
        urlBuilder.Append("/upo");

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetSessionUpoAsync(string referenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken, referenceNumber, upoReferenceNumber);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/sessions/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));
        urlBuilder.Append("/upo/");
        urlBuilder.Append(Uri.EscapeDataString(upoReferenceNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetInvoiceAsync(string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken, ksefNumber);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append("/api/v2/invoices/ksef/");
        urlBuilder.Append(Uri.EscapeDataString(ksefNumber));

        var url = urlBuilder.ToString();

        return await restClient.SendAsync<string, object>(HttpMethod.Get, url, default, accessToken, RestClient.XmlContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> DownloadInvoiceAsync(InvoiceRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<string, InvoiceRequest>(HttpMethod.Post,
                                                                    "/api/v2/invoices",
                                                                    requestPayload,
                                                                    accessToken,
                                                                    RestClient.DefaultContentType,
                                                                    cancellationToken)
                                                                    .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task<PagedInvoiceResponse> QueryInvoicesAsync(QueryInvoiceRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        var urlBuilder = new StringBuilder("/api/v2/invoices/query");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedInvoiceResponse, QueryInvoiceRequest>(HttpMethod.Post,
                                                                    urlBuilder.ToString(),
                                                                    requestPayload,
                                                                    accessToken,
                                                                    RestClient.DefaultContentType,
                                                                    cancellationToken)
                                                                    .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationStatusResponse> AsyncQueryInvoicesAsync(AsyncQueryInvoiceRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationStatusResponse, QueryInvoiceRequest>(HttpMethod.Post,
                                                                    "/api/v2/invoices/async-query",
                                                                    requestPayload,
                                                                    accessToken,
                                                                    RestClient.DefaultContentType,
                                                                    cancellationToken)
                                                                    .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AsyncQueryInvoiceStatusResponse> GetAsyncQueryInvoicesStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(operationReferenceNumber, accessToken);

        return await restClient.SendAsync<AsyncQueryInvoiceStatusResponse, object>(HttpMethod.Get,
                                                                    "/api/v2/invoices/async-query",
                                                                    default,
                                                                    accessToken,
                                                                    RestClient.DefaultContentType,
                                                                    cancellationToken)
                                                                    .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);

        var urlBuilder = new StringBuilder();

        urlBuilder.Append("/api/v2/permissions/operations/");
        urlBuilder.Append(Uri.EscapeDataString(referenceNumber));

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
        ValidateParams(permissionId, accessToken);
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
        ValidateParams(permissionId, accessToken);
        return await restClient.SendAsync<OperationResponse, string>(HttpMethod.Delete,                                                            
                                                             $"/api/v2/permissions/authorizations/grants/{Uri.EscapeDataString(permissionId)}",
                                                             default,
                                                             accessToken,
                                                             RestClient.DefaultContentType,
                                                             cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(
              PersonPermissionsQueryRequest requestPayload,
              string accessToken,
              int? pageOffset = null,
              int? pageSize = null,
              CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

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
               KSeF.Client.Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest request,
               string accessToken,
               int? pageOffset = null,
               int? pageSize = null,
               CancellationToken cancellationToken = default)
    {
        ValidatePayload(request, accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/subunits/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedPermissionsResponse<SubunitPermission>, KSeF.Client.Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest>(
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
        ValidateParams(accessToken);

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
           KSeF.Client.Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest request,
           string accessToken,
           int? pageOffset,
           int? pageSize,
           CancellationToken cancellationToken = default)
    {
        ValidatePayload(request, accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/subordinate-entities/roles");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedRolesResponse<SubordinateEntityRole>, KSeF.Client.Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest>(
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
                   KSeF.Client.Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest requestPayload,
                   string accessToken,
                   int? pageOffset,
                   int? pageSize,
                   CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        var urlBuilder = new StringBuilder("/api/v2/permissions/query/authorizations/grants");

        Pagination(pageOffset, pageSize, urlBuilder);

        return await restClient.SendAsync<PagedAuthorizationsResponse<AuthorizationGrant>, KSeF.Client.Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest>(
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
        ValidatePayload(request, accessToken);

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
        ValidatePayload(requestPayload, accessToken);

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
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse,
            GrantPermissionsEntityRequest>(HttpMethod.Post,
                                           "/api/v2/permissions/entities/grants",
                                           requestPayload,
                                           accessToken,
                                           RestClient.DefaultContentType,
                                           cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionProxyEntityAsync(GrantPermissionsProxyEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsProxyEntityRequest>(HttpMethod.Post,
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
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsIndirectEntityRequest>(HttpMethod.Post,
                                                                                       "/api/v2/permissions/indirect/grants",
                                                                                       requestPayload,
                                                                                       accessToken,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken).ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionSubUnitAsync(
        KSeF.Client.Core.Models.Permissions.SubUnit.GrantPermissionsSubUnitRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse,
            KSeF.Client.Core.Models.Permissions.SubUnit.GrantPermissionsSubUnitRequest>(HttpMethod.Post,
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
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsRequest>(
            HttpMethod.Post, "/api/v2/permissions/eu-entities/administration/grants", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync(
        GrantPermissionsEUEntitRepresentativeRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<OperationResponse, GrantPermissionsEUEntitRepresentativeRequest>(
            HttpMethod.Post, "/api/v2/permissions/eu-entities/grants", requestPayload, accessToken, RestClient.DefaultContentType, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateLimitResponse> GetCertificateLimitsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken);

        return await restClient.SendAsync<CertificateLimitResponse, object>(HttpMethod.Get, "/api/v2/certificates/limits", default, accessToken, RestClient.DefaultContentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken);

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
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<CertificateEnrollmentResponse, SendCertificateEnrollmentRequest>(HttpMethod.Post,
                                                                                                           "/api/v2/certificates/enrollments",
                                                                                                           requestPayload,
                                                                                                           accessToken,
                                                                                                           RestClient.DefaultContentType,
                                                                                                           cancellationToken)
                                                                                                         .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(accessToken, referenceNumber);

        return await restClient.SendAsync<CertificateEnrollmentStatusResponse, string>(HttpMethod.Get,
                                                                                       $"/api/v2/certificates/enrollments/{Uri.EscapeDataString(referenceNumber)}",
                                                                                       default, accessToken,
                                                                                       RestClient.DefaultContentType,
                                                                                       cancellationToken).ConfigureAwait(false);

    }

    /// <inheritdoc />
    public async Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);

        return await restClient.SendAsync<CertificateListResponse, CertificateListRequest>(HttpMethod.Post,
                                                                                           $"/api/v2/certificates/retrieve",
                                                                                           requestPayload, accessToken,
                                                                                           RestClient.DefaultContentType,
                                                                                           cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeCertificateAsync(CertificateRevokeRequest requestPayload, string serialNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidatePayload(requestPayload, accessToken);
        ValidateParams(serialNumber);

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
        ValidateParams(accessToken);

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
        ValidatePayload(requestPayload, accessToken);
        return await restClient.SendAsync<KsefTokenResponse, KsefTokenRequest>(HttpMethod.Post,
                                                                               "/api/v2/tokens",
                                                                               requestPayload,
                                                                               accessToken,
                                                                               RestClient.DefaultContentType,
                                                                               cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QueryKsefTokensResponse> QueryKsefTokensAsync(string accessToken, ICollection<AuthenticationKsefTokenStatus> statuses = null, string continuationToken = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        var urlBuilder = new StringBuilder("/api/v2/tokens?");
        if (statuses != null && statuses.Any())
        {
            urlBuilder.Append("statuses=");
            urlBuilder.Append(string.Join(",", statuses.Select(s => s.ToString())));
            urlBuilder.Append("&");
        }

        if (pageSize.HasValue)
        {
            urlBuilder.Append($"pageSize={pageSize.Value}&");
        }
        return await restClient.SendAsync<QueryKsefTokensResponse, string>(HttpMethod.Get,
                                                                           urlBuilder.ToString().TrimEnd('&'),
                                                                           default,
                                                                           accessToken,
                                                                           RestClient.DefaultContentType,
                                                                           cancellationToken,
                                                                          !string.IsNullOrWhiteSpace(continuationToken) ?
                                                                          new Dictionary<string, string> { { "x-continuation-token", continuationToken } }
                                                                          : null)
                                                                           .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task<AuthenticationKsefToken> GetKsefTokenAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);

        return await restClient.SendAsync<AuthenticationKsefToken, string>(HttpMethod.Get,
                                                                            $"/api/v2/tokens/{Uri.EscapeDataString(referenceNumber)}",
                                                                            default,
                                                                            accessToken,
                                                                            RestClient.DefaultContentType,
                                                                            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RevokeKsefTokenAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateParams(referenceNumber, accessToken);
        await restClient.SendAsync(HttpMethod.Delete,
                                   $"/api/v2/tokens/{Uri.EscapeDataString(referenceNumber)}",
                                   accessToken,
                                   RestClient.DefaultContentType,
                                   cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default)
    {
        if (parts == null || parts.Count == 0)
            throw new ArgumentException("Brak plików do wysłania.", nameof(parts));

        await SendPartsInternalAsync(openBatchSessionResponse.PartUploadRequests, parts, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendPartsInternalAsync(
     ICollection<PackagePartSignatureInitResponseType> parts,
     ICollection<BatchPartSendingInfo> batchPartSendingInfos,
     CancellationToken cancellationToken)
    {
        if (parts == null)
            throw new InvalidOperationException("Brak informacji o partach do wysłania.");

        using var httpClient = new HttpClient();
        var errors = new List<string>();

        foreach (var part in parts)
        {
            var fileInfo = batchPartSendingInfos.First(x => x.OrdinalNumber == part.OrdinalNumber);
            using var content = new ByteArrayContent(fileInfo.Data);

            // Dodaj wymagane nagłówki
            if (part.Headers != null)
            {
                foreach (var header in part.Headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }
            }

            // Przygotuj metodę i żądanie
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

            var response = await httpClient.SendAsync(request, cancellationToken);

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

    private static void ValidatePayload(object requestPayload, string accessToken)
    {
        ValidateParams(accessToken);

        ArgumentNullException.ThrowIfNull(requestPayload);
    }

    private static void ValidatePayload(object requestPayload)
    {
        ArgumentNullException.ThrowIfNull(requestPayload);
    }

    private static void ValidateParams(params string[] parameters)
    {
        foreach (var param in parameters)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentNullException(nameof(param), "Parameter cannot be null or empty.");
            }
        }
    }
}