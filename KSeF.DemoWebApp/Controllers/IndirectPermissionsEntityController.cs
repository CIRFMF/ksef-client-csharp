using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class IndirectPermissionsEntityController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-indirect-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsIndirectEntityRequest grantPermissionsRequest, CancellationToken cancellationToken)
    {
        var request = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(grantPermissionsRequest.SubjectIdentifier)
            .WithContext(grantPermissionsRequest.TargetIdentifier)
            .WithPermissions(grantPermissionsRequest.Permissions.ToArray())
            .WithDescription(grantPermissionsRequest.Description)
            .Build();

        return await ksefClient.GrantsPermissionIndirectEntityAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-indirect-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(
        string permissionId,
    string accessToken,
    CancellationToken cancellationToken)
    {
        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
