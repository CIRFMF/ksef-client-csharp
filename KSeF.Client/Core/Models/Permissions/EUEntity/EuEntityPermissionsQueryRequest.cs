namespace KSeF.Client.Core.Models.Permissions.EUEntity;

public class EuEntityPermissionsQueryRequest
{
    public string VatUeIdentifier { get; set; }
    public string AuthorizedFingerprintIdentifier { get; set; }
    public List<EuEntityPermissionsQueryPermissionType> PermissionTypes { get; set; }
}
public enum EuEntityPermissionsQueryPermissionType
{
    CredentialsManage,
    InvoiceWrite,
    InvoiceRead,
    Introspection
}
