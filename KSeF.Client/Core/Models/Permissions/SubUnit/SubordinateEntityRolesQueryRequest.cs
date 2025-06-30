namespace KSeF.Client.Core.Models.Permissions.SubUnit;

public class SubordinateEntityRolesQueryRequest
{
    public EntityPermissionsSubordinateEntityIdentifier SubordinateEntityIdentifier { get; set; }
}
public class EntityPermissionsSubordinateEntityIdentifier
{
    public ContextIdentifierType Type { get; set; }
    public string Value { get; set; }
}
