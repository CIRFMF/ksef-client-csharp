
namespace KSeF.Client.Core.Models.Authorization;

public class KsefTokenRequest
{
    /// <summary>
    /// Uprawnienia przypisane tokenowi.
    /// </summary>
    public ICollection<KsefTokenPermissionType> Permissions { get; init; }

    /// <summary>
    /// Opis tokena.
    /// </summary>
    public string Description { get; init; }
}
