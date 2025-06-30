using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class EUEntityRepresentativePermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, SubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        var request = GrantEUEntityRepresentativePermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermissions(StandardPermissionType.InvoiceRead, StandardPermissionType.InvoiceWrite)
            .WithDescription("Representative access")
            .Build();

        return await ksefClient.GrantsPermissionEUEntityRepresentativeAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string accessToken, string permissionId, CancellationToken cancellationToken)
    {
  
        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
