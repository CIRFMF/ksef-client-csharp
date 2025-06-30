namespace KSeF.Client.Core.Models.Permissions.Person;

public class GrantPermissionsPersonRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ICollection<StandardPermissionType> Permissions { get; set; }
    public string Description { get; set; }
}

public enum StandardPermissionType
{
    InvoiceRead,
    InvoiceWrite,
    Introspection,
    CredentialsRead,
    CredentialsManage,
    EnforcementOperations,
    SubunitManage
}

public partial class SubjectIdentifier
{
    public SubjectIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public enum SubjectIdentifierType
{
    Nip,
    Pesel,
    Fingerprint
}

public enum AuthorizedIdentifierType
{
    Nip,
    Pesel,
    Fingerprint
}

public enum TargetIdentifierType
{
    Nip    
}
