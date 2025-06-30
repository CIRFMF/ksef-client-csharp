using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeFClient;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Controllers;

[ApiController]
[Route("/permissions/query")]
public class SearchPermissionController(IKSeFClient ksefClient) : ControllerBase
{
    /// <summary>
    /// Pobranie listy uprawnień do pracy w KSeF nadanych osobom fizycznym / Search person permissions (granted)
    /// </summary>
    [HttpPost("persons/grants")]
    public async Task<IActionResult> SearchGrantedPersonPermissionsAsync(
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize,
        [FromQuery] string accessToken,
        [FromBody] PersonPermissionsQueryRequest request)
    {
        return Ok(await ksefClient.SearchGrantedPersonPermissionsAsync(request, accessToken, pageOffset, pageSize));

    }

    /// <summary>
    /// Pobranie listy uprawnień administratora podmiotu podrzędnego / Search subunit admin permissions
    /// </summary>
    [HttpPost("subunits/grants")]
    public async Task<IActionResult> SearchSubunitAdminPermissionsAsync(
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize,
        [FromQuery] string accessToken,
        [FromBody] SubunitPermissionsQueryRequest request)
    {
        return Ok(await ksefClient.SearchSubunitAdminPermissionsAsync(request, accessToken, pageOffset, pageSize));

    }

    /// <summary>
    /// Pobranie listy uprawnień do obsługi faktur nadanych podmiotom / Search entity invoice roles
    /// </summary>
    [HttpGet("entities/roles")]
    public async Task<IActionResult> SearchEntityInvoiceRolesAsync(
        [FromQuery] string accessToken,
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize
        )
    {
        return Ok(await ksefClient.SearchEntityInvoiceRolesAsync(accessToken, pageOffset, pageSize));

    }

    /// <summary>
    /// Pobranie listy uprawnień do obsługi faktur nadanych podmiotom podrzędnym / Search subordinate entity invoice roles
    /// </summary>
    [HttpPost("subordinate-entities/roles")]
    public async Task<IActionResult> SearchSubordinateEntityInvoiceRolesAsync(
        [FromBody] SubordinateEntityRolesQueryRequest request,
        [FromQuery] string accessToken,
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize)
    {

        return Ok(await ksefClient.SearchSubordinateEntityInvoiceRolesAsync(request, accessToken,pageOffset, pageSize));

    }

    /// <summary>
    /// Pobranie listy uprawnień o charakterze upoważnień nadanych podmiotom / Search authorization-type entity permissions
    /// </summary>
    [HttpPost("authorizations/grants")]
    public async Task<IActionResult> SearchEntityAuthorizationGrantsAsync(
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize,
        [FromQuery] string accessToken,
        [FromBody] EntityAuthorizationsQueryRequest request)
    {
        return Ok(await ksefClient.SearchEntityAuthorizationGrantsAsync(request, accessToken, pageOffset, pageSize));

    }

    /// <summary>
    /// Pobranie listy uprawnień nadanych podmiotom unijnym / Search EU entity granted permissions
    /// </summary>
    [HttpPost("eu-entities/grants")]
    public async Task<IActionResult> SearchGrantedEuEntityPermissionsAsync(
        [FromQuery] int pageOffset,
        [FromQuery] int pageSize,
        [FromQuery] string accessToken,
        [FromBody] EuEntityPermissionsQueryRequest request)
    {
        return Ok(await ksefClient.SearchGrantedEuEntityPermissionsAsync(request, accessToken, pageOffset, pageSize));
    }
}

