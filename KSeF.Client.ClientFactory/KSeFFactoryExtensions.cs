using Microsoft.Extensions.DependencyInjection;
using KSeF.Client.Http;
using System.Net.Http.Headers;

namespace KSeF.Client.ClientFactory.DI
{
    /// <summary>
    /// Extension methods do rejestracji fabryki klientów KSeF w kontenerze DI.
    /// </summary>
    public static class KSeFFactoryExtensions
    {
        /// <summary>
        /// Rejestruje wszystkie potrzebne serwisy do korzystania z KSeF w formie fabryki klientów
        /// </summary>
        /// <param name="services">Rozszerzany interfejs</param>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection RegisterKSeFClientFactory(this IServiceCollection services, bool useCamelCase = false)
        {
            if (useCamelCase)
            {
                JsonUtil.ResetConfigurationForCasePropertyName(useCamelCase);
            }

            services.AddHttpClient(Environment.Demo.ToString(), http =>
            {
                http.BaseAddress = new Uri(KsefEnvironmentConfig.BaseUrls[Environment.Demo]);
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
            services.AddHttpClient(Environment.Test.ToString(), http =>
            {
                http.BaseAddress = new Uri(KsefEnvironmentConfig.BaseUrls[Environment.Test]);
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
            services.AddHttpClient(Environment.Prod.ToString(), http =>
            {
                http.BaseAddress = new Uri(KsefEnvironmentConfig.BaseUrls[Environment.Prod]);
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddSingleton<IKSeFFactoryCryptographyServices,KSeFFactoryCryptographyServices>();
            services.AddSingleton<IKSeFClientFactory, KSeFClientFactory>();
            services.AddSingleton<IKSeFFactoryCertificateFetcherServices, KSeFFactoryCertificateFetcherServices>();

            return services;
        }
    }
}