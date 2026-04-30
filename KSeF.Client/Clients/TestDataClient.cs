using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Core.Models.TestData;

namespace KSeF.Client.Clients
{
    /// <inheritdoc />
    public sealed class TestDataClient(IRestClient rest, IRouteBuilder routeBuilder) : ClientBase(rest, routeBuilder), ITestDataClient
    {

        /// <inheritdoc />
        public Task<Status> CreateSubjectAsync(SubjectCreateRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, SubjectCreateRequest>(Routes.TestData.CreateSubject, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RemoveSubjectAsync(SubjectRemoveRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, SubjectRemoveRequest>(Routes.TestData.RemoveSubject, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> CreatePersonAsync(PersonCreateRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, PersonCreateRequest>(Routes.TestData.CreatePerson, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RemovePersonAsync(PersonRemoveRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, PersonRemoveRequest>(Routes.TestData.RemovePerson, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> GrantPermissionsAsync(TestDataPermissionsGrantRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, TestDataPermissionsGrantRequest>(Routes.TestData.GrantPerms, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RevokePermissionsAsync(TestDataPermissionsRevokeRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, TestDataPermissionsRevokeRequest>(Routes.TestData.RevokePerms, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> EnableAttachmentAsync(AttachmentPermissionGrantRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, AttachmentPermissionGrantRequest>(Routes.TestData.EnableAttach, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> DisableAttachmentAsync(AttachmentPermissionRevokeRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, AttachmentPermissionRevokeRequest>(Routes.TestData.DisableAttach, request, cancellationToken);

        /// <inheritdoc />
        public Task<Status> ChangeSessionLimitsInCurrentContextAsync(ChangeSessionLimitsInCurrentContextRequest request, string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, ChangeSessionLimitsInCurrentContextRequest>(Routes.TestData.ChangeSessionLimitsInCurrentContext, request, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RestoreDefaultSessionLimitsInCurrentContextAsync(string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status>(Routes.TestData.RestoreDefaultSessionLimitsInCurrentContext, HttpMethod.Delete, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> ChangeCertificatesLimitInCurrentSubjectAsync(ChangeCertificatesLimitInCurrentSubjectRequest request, string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, ChangeCertificatesLimitInCurrentSubjectRequest>(Routes.TestData.ChangeCertificatesLimitInCurrentSubject, request, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RestoreDefaultCertificatesLimitInCurrentSubjectAsync(string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status>(Routes.TestData.RestoreDefaultCertificatesLimitInCurrentSubject, HttpMethod.Delete, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RestoreRateLimitsAsync(string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status>(Routes.TestData.RateLimits, HttpMethod.Delete, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> SetRateLimitsAsync(EffectiveApiRateLimitsRequest requestPayload, string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync<Status, EffectiveApiRateLimitsRequest>(Routes.TestData.RateLimits, requestPayload, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> RestoreProductionRateLimitsAsync(string accessToken, CancellationToken cancellationToken = default) =>
             ExecuteAsync<Status>(Routes.TestData.ProductionRateLimits, HttpMethod.Post, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> UnblockContextAsync(ContextIdentifier requestPayload, string accessToken, CancellationToken cancellationToken = default) =>  
            ExecuteAsync<Status, ContextIdentifier>(Routes.TestData.UnblockContext, requestPayload, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<Status> BlockContextAsync(ContextIdentifier requestPayload, string accessToken, CancellationToken cancellationToken = default) =>            
            ExecuteAsync<Status, ContextIdentifier>(Routes.TestData.BlockContext, requestPayload, accessToken, cancellationToken);        

    }
}
