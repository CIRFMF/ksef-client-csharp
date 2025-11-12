using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;
namespace KSeF.Client.Api.Services.Internal;

/// <summary>
/// Domyślna implementacja interfejsu ICertificateFetcher, która pobiera
/// certyfikaty KSeF przy użyciu ICryptographyClient.
/// </summary>
/// <remarks>
/// Inicjalizuje nową instancję klasy DefaultCertificateFetcher.
/// </remarks>
/// <param name="cryptographyClient">Klient kryptograficzny, z którego będą pobierane certyfikaty.
/// Zostanie on wstrzyknięty przez kontener DI.</param>
public class DefaultCertificateFetcher(ICryptographyClient cryptographyClient) : ICertificateFetcher
{
    private readonly ICryptographyClient _cryptographyClient = cryptographyClient ?? throw new ArgumentNullException(nameof(cryptographyClient));

    /// <inheritdoc />
    public Task<ICollection<PemCertificateInfo>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        return _cryptographyClient.GetPublicCertificatesAsync(cancellationToken);
    }
}