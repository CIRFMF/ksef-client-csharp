

namespace KSeF.Client.Core.Models.Authorization;

public class AuthenticationKsefToken
{
    /// <summary>
    /// Numer referencyjny tokena.
    /// </summary>
    public string ReferenceNumber { get; set; }

    /// <summary>
    /// Identyfikator osoby która wygenerowała token.
    /// </summary>
    public SubjectIdentifier AuthorIdentifier { get; set; }
    /// <summary>
    /// Identyfikator kontekstu, w którym został wygenerowany token i do którego daje dostęp.
    /// </summary>
    public AuthContextIdentifier ContextIdentifier { get; set; }

    /// <summary>
    /// Opis tokena.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Uprawnienia przypisane tokenowi.
    /// </summary>
    public ICollection<KsefTokenPermissionType> RequestedPermissions { get; set; }

    /// <summary>
    /// Data i czas utworzenia tokena.
    /// </summary>
    public DateTimeOffset DateCreated { get; set; }
    /// <summary>
    /// Status tokena.
    /// </summary>
    public AuthenticationKsefTokenStatus Status { get; set; }
}
