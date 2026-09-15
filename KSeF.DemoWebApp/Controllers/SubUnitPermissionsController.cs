using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Api.Builders.SubEntityPermissions;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class SubUnitPermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-sub-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsSubunitRequest grantPermissionsRequest, CancellationToken cancellationToken)
    {
        GrantPermissionsSubunitRequest request = GrantSubunitPermissionsRequestBuilder
            .Create()
            .WithSubject(grantPermissionsRequest.SubjectIdentifier)
            .WithContext(grantPermissionsRequest.ContextIdentifier)
            .WithDescription(grantPermissionsRequest.Description)
            .WithSubjectDetails(grantPermissionsRequest.SubjectDetails)
            .Build();

        return await ksefClient.GrantsPermissionSubUnitAsync(request, accessToken, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("revoke-sub-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string accessToken, string permissionId, CancellationToken cancellationToken)
    {
      
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken).ConfigureAwait(false);
    }
}
