namespace KSeF.Client.Core.Models.Permissions.IndirectEntity;

public class GrantPermissionsIndirectEntityRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public TargetIdentifier TargetIdentifier { get; set; }
    public ICollection<StandardPermissionType> Permissions { get; set; }
    public string Description { get; set; }
}

public enum StandardPermissionType
{
    InvoiceRead,
    InvoiceWrite,
}

public partial class SubjectIdentifier
{
    public SubjectIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public partial class TargetIdentifier
{
    public TargetIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public enum SubjectIdentifierType
{
    Nip,
    Pesel,
    Fingerprint
}

public enum TargetIdentifierType
{
    Nip,
    InternalId
}
