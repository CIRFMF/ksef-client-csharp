namespace KSeF.Client.Core.Models.Permissions.Entity;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ICollection<StandardPermissionType> Permissions { get; set; }
}
