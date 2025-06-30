namespace KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;

public class GrantPermissionsEUEntitRepresentativeRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
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

public enum SubjectIdentifierType
{
    Fingerprint,
}
