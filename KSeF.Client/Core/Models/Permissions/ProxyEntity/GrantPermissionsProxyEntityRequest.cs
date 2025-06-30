namespace KSeF.Client.Core.Models.Permissions.ProxyEntity;

public class GrantPermissionsProxyEntityRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public StandardPermissionType Permission { get; set; }
    public string Description { get; set; }
}

public enum StandardPermissionType
{
    SelfInvoicing,
    TaxRepresentative,
    RrInvoicing,
}

public partial class SubjectIdentifier
{
    public SubjectIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public enum SubjectIdentifierType
{
    Nip
}
