namespace KSeF.Client.Core.Models.Permissions.EUEntity;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ContextIdentifier ContextIdentifier { get; set; }
}
