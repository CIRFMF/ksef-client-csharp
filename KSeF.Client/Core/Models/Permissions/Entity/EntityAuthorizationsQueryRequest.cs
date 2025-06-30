namespace KSeF.Client.Core.Models.Permissions.Entity;

public class EntityAuthorizationsQueryRequest
{
    public EntityAuthorizationsAuthorizingEntityIdentifier AuthorizingIdentifier { get; set; }
    public EntityAuthorizationsAuthorizedEntityIdentifier AuthorizedIdentifier { get; set; }
    public QueryType QueryType { get; set; }
    public List<InvoicePermissionType> PermissionTypes { get; set; }
}
public class EntityAuthorizationsAuthorizingEntityIdentifier
{
    public string Type { get; set; }
    public string Value { get; set; }
}
public class EntityAuthorizationsAuthorizedEntityIdentifier
{
    public string Type { get; set; }
    public string Value { get; set; }
}
public enum QueryType
{
    Granted,
    Received
}
public enum InvoicePermissionType
{
    SelfInvoicing,
    TaxRepresentative
}
