namespace KSeF.Client.Core.Models.Permissions.ProxyEntity;

public class ProxyEntitySubunitPermissionsQueryRequest
{
    public SubunitPermissionsSubunitIdentifier SubunitIdentifier { get; set; }
}
public class SubunitPermissionsSubunitIdentifier
{
    public string Type { get; set; }
    public string Value { get; set; }
}
