using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class EUEntityRepresentativePermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsEuEntityRepresentativeRequest request, CancellationToken cancellationToken)
    {
        return await ksefClient.GrantsPermissionEUEntityRepresentativeAsync(request, accessToken, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("revoke-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string accessToken, string permissionId, CancellationToken cancellationToken)
    {
  
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken).ConfigureAwait(false);
    }
}
