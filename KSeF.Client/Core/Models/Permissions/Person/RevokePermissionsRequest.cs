namespace KSeF.Client.Core.Models.Permissions.Person;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ICollection<StandardPermissionType> Permissions { get; set; }
}
