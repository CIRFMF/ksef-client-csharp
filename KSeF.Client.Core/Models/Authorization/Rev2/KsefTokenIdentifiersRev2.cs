namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Identyfikator osoby, która wygenerowała token (Rev2).
    /// </summary>
    public class KsefTokenAuthorIdentifierRev2 : ExtensibleIdentifier<KsefTokenAuthorIdentifierTypeCodeRev2>
    {
    }

    /// <summary>
    /// Kody typu identyfikatora autora tokena (Rev2).
    /// </summary>
    public enum KsefTokenAuthorIdentifierTypeCodeRev2
    {
        /// <summary>
        /// Numer identyfikacji podatkowej (NIP).
        /// </summary>
        Nip,

        /// <summary>
        /// Numer PESEL.
        /// </summary>
        Pesel,

        /// <summary>
        /// Odcisk palca certyfikatu.
        /// </summary>
        Fingerprint,

        /// <summary>
        /// Typ nieznany w bieżącej rewizji operacji. Rzeczywisty kod jest dostępny w polu <c>rawCode</c>.
        /// </summary>
        FutureValue
    }

    /// <summary>
    /// Identyfikator kontekstu, w którym został wygenerowany token (Rev2).
    /// </summary>
    public class KsefTokenContextIdentifierRev2 : ExtensibleIdentifier<KsefTokenContextIdentifierTypeCodeRev2>
    {
    }

    /// <summary>
    /// Kody typu identyfikatora kontekstu tokena (Rev2).
    /// </summary>
    public enum KsefTokenContextIdentifierTypeCodeRev2
    {
        /// <summary>
        /// Numer identyfikacji podatkowej (NIP).
        /// </summary>
        Nip,

        /// <summary>
        /// Identyfikator wewnętrzny.
        /// </summary>
        InternalId,

        /// <summary>
        /// Dwuczłonowy identyfikator składający się z numeru NIP i numeru VAT-UE: {nip}-{vat_ue}.
        /// </summary>
        NipVatUe,

        /// <summary>
        /// Identyfikator dostawcy usług Peppol.
        /// </summary>
        PeppolId,

        /// <summary>
        /// Typ nieznany w bieżącej rewizji operacji. Rzeczywisty kod jest dostępny w polu <c>rawCode</c>.
        /// </summary>
        FutureValue
    }
}
