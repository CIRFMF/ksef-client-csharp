using KSeF.Client.Core.Models.Permissions;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class OperationStatusController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpGet("{referenceNumber}/status")]
    public async Task<ActionResult<PermissionsOperationStatusResponse>> GetOperationStatusAsync([FromRoute] string referenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        var status = await ksefClient.OperationsStatusAsync(referenceNumber, accessToken, cancellationToken); 
        return Ok(status); 
    }
}
