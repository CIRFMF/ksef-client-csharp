namespace KSeF.Client.Core.Models.Authorization;

public class KsefTokenResponse
{
    /// <summary>
    /// Numer referencyjny tokena. Za jego pomocą można sprawdzić jego status lub go unieważnić.
    /// </summary>
    public string ReferenceNumber { get; set; }

    /// <summary>
    /// Token uwierzytelniający.
    /// </summary>
    public string Token { get; set; }
}
