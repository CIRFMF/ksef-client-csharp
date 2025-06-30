namespace KSeF.Client.Core.Models.Permissions.SubUnit;

public class GrantPermissionsSubUnitRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ContextIdentifier ContextIdentifier { get; set; }
    public string Description { get; set; }
}

public partial class SubjectIdentifier
{
    public SubjectIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public partial class ContextIdentifier
{
    public ContextIdentifierType Type { get; set; }
    public string Value { get; set; }
}

public enum SubjectIdentifierType
{
    Nip,
    Pesel,
    Fingerprint
}

public enum ContextIdentifierType
{
    Nip,
    InternalId,
}
