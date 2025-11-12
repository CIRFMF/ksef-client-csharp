using KSeF.Client.Core.Models.Certificates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// Interfejs dla usług dostarczających certyfikaty klucza publicznego KSeF.
    /// </summary>
    public interface ICertificateFetcher
    {
        /// <summary>
        /// Pobiera kolekcję certyfikatów PEM.
        /// </summary>
        /// <param name="cancellationToken">Token do anulowania operacji.</param>
        /// <returns>Kolekcja informacji o certyfikatach.</returns>
        Task<ICollection<PemCertificateInfo>> GetCertificatesAsync(CancellationToken cancellationToken);
    }
}