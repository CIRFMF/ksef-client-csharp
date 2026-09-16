using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.CollectiveIdentifiers;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Peppol;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Token;

namespace KSeF.DemoWebApp.Tests;

/// <summary>
/// Operacje <see cref="KSeF.Client.Core.Interfaces.Clients.IKSeFClient"/> nieużywane
/// w testach sesji interaktywnej. Wydzielone do osobnego pliku, aby nie zaciemniać
/// właściwej atrapy.
/// </summary>
internal sealed partial class FakeOnlineSessionKsefClient
{
    public Task<AuthenticationListResponse> GetActiveSessions(string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task RevokeCurrentSessionAsync(string token, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task RevokeSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<AuthenticationChallengeResponse> GetAuthChallengeAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SignatureResponse> SubmitXadesAuthRequestAsync(string signedXML, bool verifyCertificateChain = false, bool enforceXadesCompliance = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SignatureResponse> SubmitKsefTokenAuthRequestAsync(AuthenticationKsefTokenRequest requestPayload, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<AuthStatus> GetAuthStatusAsync(string authOperationReferenceNumber, string authenticationToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<AuthenticationOperationStatusResponse> GetAccessTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<RefreshTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OpenBatchSessionResponse> OpenBatchSessionAsync(OpenBatchSessionRequest requestPayload, string accessToken, string upoVersion = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task CloseBatchSessionAsync(string batchSessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SendBatchPartsAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartSendingInfo> parts, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SendBatchPartsWithStreamAsync(OpenBatchSessionResponse openBatchSessionResponse, ICollection<BatchPartStreamSendingInfo> parts, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateLimitResponse> GetCertificateLimitsAsync(string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync(string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(SendCertificateEnrollmentRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync(string certificateRequestReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task RevokeCertificateAsync(CertificateRevokeRequest requestPayload, string serialNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CertificateMetadataListResponse> GetCertificateMetadataListAsync(string accessToken, CertificateMetadataListRequest requestPayload = null, int? pageSize = null, int? pageOffset = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<GenerateCollectiveIdentifierResponse> GenerateCollectiveIdentifierAsync(GenerateCollectiveIdentifierRequest request, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CollectiveIdentifiersQueryResponse> QueryCollectiveIdentifiersAsync(CollectiveIdentifiersQueryRequest request, string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CollectiveIdentifiersByKsefNumberQueryResponse> GetCollectiveIdentifiersByKsefNumberAsync(string ksefNumber, string accessToken, string continuationToken = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CollectiveIdentifierInvoicesQueryResponse> GetCollectiveIdentifierInvoicesAsync(CollectiveIdentifierInvoicesQueryRequest request, string accessToken, string continuationToken = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionPersonAsync(GrantPermissionsPersonRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionEntityAsync(GrantPermissionsEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsAuthorizationPermissionAsync(GrantPermissionsAuthorizationRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionIndirectEntityAsync(GrantPermissionsIndirectEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionSubUnitAsync(GrantPermissionsSubunitRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionEUEntityAsync(GrantPermissionsEuEntityRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> GrantsPermissionEUEntityRepresentativeAsync(GrantPermissionsEuEntityRepresentativeRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetInvoiceAsync(string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedInvoiceResponse> QueryInvoiceMetadataAsync(InvoiceQueryFilters requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, SortOrder sortOrder = SortOrder.Asc, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> ExportInvoicesAsync(InvoiceExportRequest requestPayload, string accessToken, bool includeMetadata = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> ExportInvoicesAsync(InvoiceExportRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<InvoiceExportStatusResponse> GetInvoiceExportStatusAsync(string referenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<KsefTokenResponse> GenerateKsefTokenAsync(KsefTokenRequest requestPayload, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<QueryKsefTokensResponse> QueryKsefTokensAsync(string accessToken, ICollection<AuthenticationKsefTokenStatus> statuses = null, string authorIdentifier = null, TokenContextIdentifierType? authorIdentifierType = null, string description = null, string continuationToken = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<AuthenticationKsefToken> GetKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task RevokeKsefTokenAsync(string tokenReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<QueryPeppolProvidersResponse> QueryPeppolProvidersAsync(string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PermissionsOperationStatusResponse> OperationsStatusAsync(string operationReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PermissionsAttachmentAllowedResponse> GetAttachmentPermissionStatusAsync(string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> RevokeCommonPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OperationResponse> RevokeAuthorizationsPermissionAsync(string permissionId, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedPermissionsResponse<PersonalPermission>> SearchGrantedPersonalPermissionsAsync(PersonalPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedPermissionsResponse<PersonPermission>> SearchGrantedPersonPermissionsAsync(PersonPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedPermissionsResponse<SubunitPermission>> SearchSubunitAdminPermissionsAsync(SubunitPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedRolesResponse<EntityRole>> SearchEntityInvoiceRolesAsync(string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedRolesResponse<SubordinateEntityRole>> SearchSubordinateEntityInvoiceRolesAsync(SubordinateEntityRolesQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedAuthorizationsResponse<AuthorizationGrant>> SearchEntityAuthorizationGrantsAsync(EntityAuthorizationsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedPermissionsResponse<EuEntityPermission>> SearchGrantedEuEntityPermissionsAsync(EuEntityPermissionsQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<EntityPermissionGrantResponse> QueryEntitiesGrantsAsync(EntityPermissionGrantQueryRequest requestPayload, string accessToken, int? pageOffset = null, int? pageSize = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SessionsListResponse> GetSessionsAsync(SessionType sessionType, string accessToken, int? pageSize, string continuationToken, SessionsFilter sessionsFilter = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SessionStatusResponse> GetSessionStatusAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SessionInvoicesResponse> GetSessionInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize = null, string continuationToken = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SessionInvoice> GetSessionInvoiceAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SessionInvoicesResponse> GetSessionFailedInvoicesAsync(string sessionReferenceNumber, string accessToken, int? pageSize, string continuationToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetSessionInvoiceUpoByKsefNumberAsync(string sessionReferenceNumber, string ksefNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetSessionInvoiceUpoByReferenceNumberAsync(string sessionReferenceNumber, string invoiceReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetSessionUpoAsync(string sessionReferenceNumber, string upoReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> GetUpoAsync(Uri uri, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
