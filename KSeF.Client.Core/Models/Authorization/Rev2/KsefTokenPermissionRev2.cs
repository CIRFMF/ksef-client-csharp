namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Uprawnienie przypisane tokenowi w kontrakcie Rev2 (rozszerzalna wartość wyliczeniowa <c>code</c>/<c>rawCode</c>/<c>displayName</c>).
    /// </summary>
    public class KsefTokenPermissionRev2 : ExtensibleEnumValue<KsefTokenPermissionCodeRev2>
    {
    }

    /// <summary>
    /// Kody uprawnień tokena KSeF w odpowiedziach Rev2.
    /// </summary>
    public enum KsefTokenPermissionCodeRev2
    {
        InvoiceRead,
        InvoiceWrite,
        CredentialsRead,
        CredentialsManage,
        SubunitManage,
        EnforcementOperations,
        Introspection,
        CollectiveIdentifierManage,

        /// <summary>
        /// Uprawnienie nieznane w bieżącej rewizji operacji. Rzeczywisty kod jest dostępny w polu <c>rawCode</c>.
        /// </summary>
        FutureValue
    }
}
