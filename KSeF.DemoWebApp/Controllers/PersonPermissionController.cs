using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class PersonPermissionController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-permissions-for-person")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsPerson(string accessToken, KSeF.Client.Core.Models.Permissions.Person.SubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermissions(KSeF.Client.Core.Models.Permissions.Person.StandardPermissionType.InvoiceRead, KSeF.Client.Core.Models.Permissions.Person.StandardPermissionType.InvoiceWrite)
            .WithDescription("Access for quarterly review")
            .Build();

        return await ksefClient.GrantsPermissionPersonAsync(request,  accessToken, cancellationToken);
    }

    [HttpPost("revoke-permissions-for-person")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsPerson(
    string accessToken,
    string permissionId,
    CancellationToken cancellationToken)
    {
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
