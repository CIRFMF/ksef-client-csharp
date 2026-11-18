using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.EUEntity
{
    public class EuEntityPermissionsQueryRequest
    {
        public string VatUeIdentifier { get; set; }
        public string AuthorizedFingerprintIdentifier { get; set; }
        public List<EuEntityPermissionType> PermissionTypes { get; set; }
    }

    public enum EuEntityPermissionType
    {
        VatUeManage,
        InvoiceWrite,
        InvoiceRead,
        Introspection,
        CollectiveIdentifierManage
    }
}
