using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OperationsController(IKSeFClient client) : ControllerBase
    {        
        /// <summary>
        /// Sprawdzenie statusu zgody na wystawianie faktur z załącznikiem.
        /// </summary>
        [HttpGet("attachments/status")]
        [ProducesResponseType(typeof(PermissionsAttachmentAllowedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PermissionsAttachmentAllowedResponse>> GetAttachmentPermissionStatus(
            string accessToken,
            CancellationToken cancellationToken)
        {
            PermissionsAttachmentAllowedResponse result = await client.GetAttachmentPermissionStatusAsync(accessToken, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
    }
}
