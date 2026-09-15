using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class IndirectPermissionsEntityController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-indirect-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsIndirectEntityRequest grantPermissionsRequest, CancellationToken cancellationToken)
    {
        GrantPermissionsIndirectEntityRequest request = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(grantPermissionsRequest.SubjectIdentifier)
            .WithContext(grantPermissionsRequest.TargetIdentifier)
            .WithPermissions([.. grantPermissionsRequest.Permissions])
            .WithDescription(grantPermissionsRequest.Description)
            .WithSubjectDetails(grantPermissionsRequest.SubjectDetails)
            .Build();

        return await ksefClient.GrantsPermissionIndirectEntityAsync(request, accessToken, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("revoke-indirect-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(
        string permissionId,
    string accessToken,
    CancellationToken cancellationToken)
    {
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken).ConfigureAwait(false);
    }
}
