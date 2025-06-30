using KSeF.Client.Api.Builders.SubUnitPermissions;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.Permissions;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class SubUnitPermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-sub-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsSubUnitRequest grantPermissionsRequest, CancellationToken cancellationToken)
    {
        var request = GrantSubUnitPermissionsRequestBuilder
            .Create()
            .WithSubject(grantPermissionsRequest.SubjectIdentifier)
            .WithContext(grantPermissionsRequest.ContextIdentifier)
            .WithDescription(grantPermissionsRequest.Description)
            .Build();

        return await ksefClient.GrantsPermissionSubUnitAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-sub-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string accessToken, string permissionId, CancellationToken cancellationToken)
    {
      
        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
