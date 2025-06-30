using KSeF.Client.Core.Models.Permissions.Person;
using System.Text.Json.Serialization;

namespace KSeF.Client.Core.Models.Permissions;

public class PersonPermission
{
    public string Id { get; set; }
    public string AuthorizedIdentifier { get; set; }
    public string AuthorizedIdentifierType { get; set; }
    public string SubunitIdentifier { get; set; }
    public string SubunitdentifierType { get; set; } 
    public string SubunitIdentifierType { get; set; }
    
    [JsonIgnore]
    public string SubunitIdentifierTypeGet =>  !string.IsNullOrEmpty(SubunitdentifierType)
            ? SubunitdentifierType 
            : SubunitIdentifierType;

    public string AuthorIdentifier { get; set; }
    public string AuthorIdentifierType { get; set; }
    public string PermissionScope { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public bool CanDelegate { get; set; }
}

public class SubunitPermission
{
    public string Id { get; set; }
    public string AuthorizedIdentifier { get; set; }
    public string AuthorizedIdentifierType { get; set; }
    public string SubunitIdentifier { get; set; }
    public string SubunitIdentifierType { get; set; }
    public string AuthorIdentifier { get; set; }
    public string AuthorIdentifierType { get; set; }
    public StandardPermissionType PermissionScope { get; set; }
    public string Description { get; set; }
    public DateTimeOffset StartDate { get; set; }
}

public class EntityRole
{
    public string ParentEntityIdentifier { get; set; }
    public string ParentEntityIdentifierType { get; set; }
    public string Role { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
}

public class SubordinateEntityRole 
{
    public string SubordinateEntityIdentifier { get; set; }
    public string SubordinateEntityIdentifierType { get; set; }
    public string Role { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
}

public class EuEntityPermission
{
    public string Id { get; set; }
    public string AuthorIdentifier { get; set; }
    public string AuthorIdentifierType { get; set; }
    public string VatUeIdentifier { get; set; }
    public string EuEntityName { get; set; }
    public string AuthorizedFingerprintIdentifier { get; set; }
    public string PermissionType { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
}

public class AuthorizationGrant 
{
    public string Id { get; set; }
    public string AuthorIdentifier { get; set; }
    public string AuthorIdentifierType { get; set; }
    public string AuthorizedEntityIdentifier { get; set; }
    public string AuthorizedEntityIdentifierType { get; set; }
    public string AuthorizingEntityIdentifier { get; set; }
    public string AuthorizingEntityIdentifierType { get; set; }
    public string AuthorizationScope { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
}
