namespace KSeF.Client.Core.Models.Permissions.SubUnit;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ContextIdentifier ContextIdentifier { get; set; }
}
