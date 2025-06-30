namespace KSeF.Client.Core.Models.Permissions.SubUnit;

public class SubunitPermissionsQueryRequest
{
    public SubunitPermissionsSubunitIdentifier SubunitIdentifier { get; set; }
}
public class SubunitPermissionsSubunitIdentifier
{
    public SubunitIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public enum SubunitIdentifierType
{
    InternalId
}
