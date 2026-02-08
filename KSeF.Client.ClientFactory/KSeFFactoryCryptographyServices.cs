#nullable enable
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Http;

namespace KSeF.Client.ClientFactory
{
    /// <summary>
    /// Fabryka serwisów kryptograficznych KSeF.
    /// </summary>
    /// <remarks>
    /// Udostępnia instancje <see cref="ICryptographyService"/> oraz <see cref="ICryptographyClient"/> dla wskazanych środowisk.
    /// Mechanizm cache pozwala tworzyć serwisy tylko raz dla danego środowiska.
    /// Obsługiwane środowiska: Test, Demo, Prod.
    /// </remarks>
    public interface IKSeFFactoryCryptographyServices
    {
        /// <summary>
        /// Pobiera lub tworzy instancję <see cref="ICryptographyService"/> dla wskazanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF (<see cref="Environment.Test"/>, <see cref="Environment.Demo"/>, <see cref="Environment.Prod"/>).</param>
        /// <returns>Zadanie zwracające instancję <see cref="ICryptographyService"/>.</returns>
        Task<ICryptographyService> CryprographyService(Environment environment);

        /// <summary>
        /// Tworzy instancję <see cref="ICryptographyClient"/> dla podanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego tworzony jest klient.</param>
        /// <returns>Instancja <see cref="ICryptographyClient"/>.</returns>
        ICryptographyClient CryptographyClient(Environment environment);

        /// <summary>
        /// Czyści cache wszystkich instancji <see cref="ICryptographyService"/> dla wszystkich środowisk.
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Implementacja fabryki serwisów kryptograficznych KSeF.
    /// </summary>
    /// <remarks>
    /// Klasa tworzy instancje serwisów kryptograficznych z mechanizmem cache i synchronizacją.
    /// Dzięki temu każda instancja <see cref="ICryptographyService"/> dla danego środowiska
    /// jest tworzona tylko raz. Serwis umożliwia pobieranie certyfikatów publicznych
    /// oraz wykonywanie operacji kryptograficznych wymaganych przez KSeF.
    /// </remarks>
    public class KSeFFactoryCryptographyServices(IHttpClientFactory _factory) : IKSeFFactoryCryptographyServices
    {
        private Task<ICryptographyService>? demoCryptographyService;
        private readonly object demoCryptographyServiceLock = new();
        private Task<ICryptographyService>? prodCryptographyService;
        private readonly object prodCryptographyServiceLock = new();
        private Task<ICryptographyService>? testCryptographyService;
        private readonly object testCryptographyServiceLock = new();

        /// <summary>
        /// Pobiera lub tworzy instancję <see cref="ICryptographyService"/> dla danego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego ma zostać pobrany serwis kryptograficzny.</param>
        /// <returns>Zadanie zwracające instancję <see cref="ICryptographyService"/>.</returns>
        /// <remarks>
        /// Mechanizm synchronizacji zapobiega wielokrotnemu tworzeniu instancji w tym samym czasie.
        /// Jeśli serwis dla danego środowiska nie istnieje w cache, tworzony jest nowy
        /// oraz inicjalizowany metodą <c>WarmupAsync()</c>.
        /// </remarks>
        public async Task<ICryptographyService> CryprographyService(Environment environment)
        {
            ICryptographyClient cryptographyService = CryptographyClient(environment);
            Task<ICryptographyService> outCryptographyService = default(Task<ICryptographyService>);

            object cryptographyServiceLock;
            Task<ICryptographyService>? serviceRef;

            switch (environment)
            {
                case Environment.Demo:
                    cryptographyServiceLock = demoCryptographyServiceLock;
                    serviceRef = demoCryptographyService;
                    break;

                case Environment.Prod:
                    cryptographyServiceLock = prodCryptographyServiceLock;
                    serviceRef = prodCryptographyService;
                    break;

                case Environment.Test:
                    cryptographyServiceLock = testCryptographyServiceLock;
                    serviceRef = testCryptographyService;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }

            lock (cryptographyServiceLock)
            {
                if (serviceRef is null)
                {
                    CryptographyService newCryptographyService = new CryptographyService(cryptographyService.GetPublicCertificatesAsync);
                    serviceRef = newCryptographyService.WarmupAsync()
                                   .ContinueWith(_ => (ICryptographyService)newCryptographyService);

                    switch (environment)
                    {
                        case Environment.Demo:
                            demoCryptographyService = serviceRef;
                            break;

                        case Environment.Prod:
                            prodCryptographyService = serviceRef;
                            break;

                        case Environment.Test:
                            testCryptographyService = serviceRef;
                            break;
                    }
                }

                outCryptographyService = serviceRef;
            }

            return await outCryptographyService.ConfigureAwait(false);
        }

        /// <summary>
        /// Tworzy instancję <see cref="ICryptographyClient"/> dla podanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego tworzony jest klient.</param>
        /// <returns>Instancja <see cref="ICryptographyClient"/>.</returns>
        public ICryptographyClient CryptographyClient(Environment environment)
        {
            RestClient restClient = new RestClient(_factory.CreateClient(environment.ToString()));
            return new global::KSeF.Client.Clients.CryptographyClient(restClient);
        }

        /// <summary>
        /// Czyści cache wszystkich instancji <see cref="ICryptographyService"/> dla wszystkich środowisk.
        /// </summary>
        public void ClearCache()
        {
            lock (demoCryptographyServiceLock)
            {
                demoCryptographyService = null;
            }

            lock (prodCryptographyServiceLock)
            {
                prodCryptographyService = null;
            }

            lock (testCryptographyServiceLock)
            {
                testCryptographyService = null;
            }
        }
    }
}
