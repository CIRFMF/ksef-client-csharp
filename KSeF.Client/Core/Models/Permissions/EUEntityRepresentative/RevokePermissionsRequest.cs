namespace KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ICollection<StandardPermissionType> Permissions { get; set; }
}
