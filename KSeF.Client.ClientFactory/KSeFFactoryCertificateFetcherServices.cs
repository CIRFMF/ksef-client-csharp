#nullable enable
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.ClientFactory
{
    /// <summary>
    /// Fabryka serwisów odpowiedzialnych za pobieranie certyfikatów KSeF.
    /// </summary>
    /// <remarks>
    /// Umożliwia tworzenie i przechowywanie instancji <see cref="ICertificateFetcher"/> dla różnych środowisk.
    /// Obsługuje cache, dzięki czemu dla danego środowiska certyfikat jest tworzony tylko raz.
    /// </remarks>
    public interface IKSeFFactoryCertificateFetcherServices
    {
        /// <summary>
        /// Pobiera lub tworzy instancję <see cref="ICertificateFetcher"/> dla wskazanego środowiska.
        /// </summary>
        /// <param name="environment">
        /// Środowisko KSeF (<see cref="Environment.Test"/>, <see cref="Environment.Demo"/>, <see cref="Environment.Prod"/>).
        /// </param>
        /// <param name="customFetcher">
        /// Opcjonalny niestandardowy serwis certyfikatów. Jeśli nie zostanie podany, użyty zostanie domyślny.
        /// </param>
        /// <returns>
        /// Zadanie zwracające instancję <see cref="ICertificateFetcher"/> dla danego środowiska.
        /// </returns>
        Task<ICertificateFetcher> GetOrSetCertificateFetcher(Environment environment, Task<ICertificateFetcher>? customFetcher = null);

        /// <summary>
        /// Czyści pamięć podręczną instancji <see cref="ICertificateFetcher"/> dla wskazanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego pamięć podręczna ma zostać wyczyszczona.</param>
        void Invalidate(Environment environment);

        /// <summary>
        /// Czyści pamięć podręczną instancji <see cref="ICertificateFetcher"/> dla wskazanego środowiska
        /// i od razu tworzy jego nową instancję dla tego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego pamięć podręczna ma zostać odświeżona.</param>
        /// <param name="customFetcher">
        /// Opcjonalny niestandardowy serwis certyfikatów. Jeśli nie zostanie podany, użyty zostanie domyślny.
        /// </param>
        /// <returns>Nowa instancja <see cref="ICertificateFetcher"/> po odświeżeniu pamięci podręcznej.</returns>
        Task<ICertificateFetcher> RefreshAsync(Environment environment, Task<ICertificateFetcher>? customFetcher = null);

        /// <summary>
        /// Czyści pamięć podręczną instancji <see cref="ICertificateFetcher"/> dla wszystkich środowisk.
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Implementacja fabryki serwisów pobierających certyfikaty KSeF.
    /// </summary>
    /// <remarks>
    /// Fabryka wykorzystuje mechanizm pamięci podręcznej (cache), aby instancje <see cref="ICertificateFetcher"/>
    /// były tworzone tylko raz dla każdego środowiska. Obsługuje środowiska: Test, Demo, Prod.
    /// Dodatkowo obsługuje problemy z certyfikatem na podstawie kodów błędów z KSeF.
    /// </remarks>
    public class KSeFFactoryCertificateFetcherServices(
        IKSeFFactoryCryptographyServices kSeFFactoryCryptographyServices)
        : IKSeFFactoryCertificateFetcherServices
    {
        private Task<ICertificateFetcher>? demoFetcherService;
        private readonly object demoFetcherServiceLock = new();

        private Task<ICertificateFetcher>? prodFetcherService;
        private readonly object prodFetcherServiceLock = new();

        private Task<ICertificateFetcher>? testFetcherService;
        private readonly object testFetcherServiceLock = new();

        /// <summary>
        /// Pobiera lub tworzy instancję <see cref="ICertificateFetcher"/> dla podanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego ma zostać pobrany certyfikat.</param>
        /// <param name="customFetcher">
        /// Opcjonalny niestandardowy serwis certyfikatów. Umożliwia wczytywanie do pamięci podręcznej własnych
        /// instancji dziedziczących po <see cref="ICertificateFetcher"/>.
        /// </param>
        /// <returns>Zadanie zwracające instancję <see cref="ICertificateFetcher"/>.</returns>
        /// <remarks>
        /// Jeśli instancja dla danego środowiska nie istnieje w pamięci podręcznej, zostaje utworzona jego domyślna forma.
        /// Mechanizm synchronizacji zapobiega wielokrotnemu tworzeniu instancji w tym samym czasie.
        /// </remarks>
        public async Task<ICertificateFetcher> GetOrSetCertificateFetcher(
            Environment environment,
            Task<ICertificateFetcher>? customFetcher = null)
        {
            Task<ICertificateFetcher> outCertificateService;

            object certificateFetcherServiceLock;
            Task<ICertificateFetcher>? serviceRef;

            switch (environment)
            {
                case Environment.Demo:
                    certificateFetcherServiceLock = demoFetcherServiceLock;
                    serviceRef = demoFetcherService;
                    break;

                case Environment.Prod:
                    certificateFetcherServiceLock = prodFetcherServiceLock;
                    serviceRef = prodFetcherService;
                    break;

                case Environment.Test:
                    certificateFetcherServiceLock = testFetcherServiceLock;
                    serviceRef = testFetcherService;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }

            lock (certificateFetcherServiceLock)
            {
                if (serviceRef is null)
                {
                    serviceRef = customFetcher ?? DefaultCertificateFetcher(environment);

                    switch (environment)
                    {
                        case Environment.Demo:
                            demoFetcherService = serviceRef;
                            break;

                        case Environment.Prod:
                            prodFetcherService = serviceRef;
                            break;

                        case Environment.Test:
                            testFetcherService = serviceRef;
                            break;
                    }
                }

                outCertificateService = serviceRef;
            }

            return await outCertificateService.ConfigureAwait(false);
        }

        /// <summary>
        /// Tworzy domyślną instancję <see cref="ICertificateFetcher"/> dla podanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego tworzony jest certyfikat.</param>
        /// <returns>Zadanie zwracające domyślny <see cref="ICertificateFetcher"/>.</returns>
        private async Task<ICertificateFetcher> DefaultCertificateFetcher(Environment environment)
        {
            ICryptographyClient cryptographyService =
                kSeFFactoryCryptographyServices.CryptographyClient(environment);

            return new global::KSeF.Client.Api.Services.Internal.DefaultCertificateFetcher(cryptographyService);
        }

        /// <summary>
        /// Czyści pamięć podręczną instancji <see cref="ICertificateFetcher"/> dla wskazanego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego pamięć podręczna ma zostać wyczyszczona.</param>
        public void Invalidate(Environment environment)
        {
            switch (environment)
            {
                case Environment.Demo:
                    lock (demoFetcherServiceLock)
                    {
                        demoFetcherService = null;
                    }
                    break;

                case Environment.Prod:
                    lock (prodFetcherServiceLock)
                    {
                        prodFetcherService = null;
                    }
                    break;

                case Environment.Test:
                    lock (testFetcherServiceLock)
                    {
                        testFetcherService = null;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }

        /// <summary>
        /// Czyści pamięć podręczną instancji <see cref="ICertificateFetcher"/> dla wskazanego środowiska
        /// i od razu tworzy jego nową instancję dla tego środowiska.
        /// </summary>
        /// <param name="environment">Środowisko KSeF, dla którego pamięć podręczna ma zostać odświeżona.</param>
        /// <param name="customFetcher">
        /// Opcjonalny niestandardowy serwis certyfikatów. Jeśli nie zostanie podany, użyty zostanie domyślny.
        /// </param>
        /// <returns>Nowa instancja <see cref="ICertificateFetcher"/> po odświeżeniu pamięci podręcznej.</returns>
        public async Task<ICertificateFetcher> RefreshAsync(
            Environment environment,
            Task<ICertificateFetcher>? customFetcher = null)
        {
            Invalidate(environment);
            return await GetOrSetCertificateFetcher(environment, customFetcher).ConfigureAwait(false);
        }

        /// <summary>
        /// Czyści pamięć podręcznąwszystkich instancji <see cref="ICertificateFetcher"/> dla wszystkich środowisk.
        /// </summary>
        public void ClearCache()
        {
            lock (demoFetcherServiceLock)
            {
                demoFetcherService = null;
            }

            lock (prodFetcherServiceLock)
            {
                prodFetcherService = null;
            }

            lock (testFetcherServiceLock)
            {
                testFetcherService = null;
            }
        }
    }
}