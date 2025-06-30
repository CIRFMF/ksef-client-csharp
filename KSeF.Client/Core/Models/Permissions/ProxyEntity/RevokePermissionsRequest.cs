namespace KSeF.Client.Core.Models.Permissions.ProxyEntity;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public StandardPermissionType Permission { get; set; }
}
