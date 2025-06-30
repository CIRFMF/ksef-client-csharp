using KSeF.Client.Api.Builders.ProxyEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.ProxyEntity;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class ProxyPermissionsEntityController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-proxy-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, SubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        var request = GrantProxyEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermission(StandardPermissionType.TaxRepresentative)
            .WithDescription("Access for quarterly review")
            .Build();

        return await ksefClient.GrantsPermissionProxyEntityAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-proxy-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(
    string accessToken,
    string permissionId,
    CancellationToken cancellationToken)
    {

        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
