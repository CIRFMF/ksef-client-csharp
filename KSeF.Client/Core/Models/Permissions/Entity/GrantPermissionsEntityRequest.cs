
namespace KSeF.Client.Core.Models.Permissions.Entity;

public class GrantPermissionsEntityRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public ICollection<Permission> Permissions { get; set; }
    public string Description { get; set; }
}

public enum StandardPermissionType
{
    InvoiceRead,
    InvoiceWrite,
}

public class Permission
{
    public StandardPermissionType Type { get; init; }
    public bool CanDelegate { get; init; }

    public Permission(StandardPermissionType type, bool canDelegate)
    {
        Type = type;
        CanDelegate = canDelegate;
    }

    public static Permission New(StandardPermissionType invoiceRead, bool canDelegate)
    {
        return new Permission(invoiceRead, canDelegate);
    }
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
