namespace KSeF.Client.Core.Models.Permissions;


public class PagedPermissionsResponse<TPermission>
{
    public List<TPermission> Permissions { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PagedAuthorizationsResponse<TAuthorization>
{
    public List<TAuthorization> AuthorizationGrants { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PagedRolesResponse<TRole>
{
    public List<TRole> Roles { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
