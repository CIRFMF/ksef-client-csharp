using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Extensions;
using KSeF.Client.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
namespace KSeF.Client.DI;

/// <summary>
/// Extension methods do rejestracji KSeF SDK w kontenerze DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje wszystkie potrzebne serwisy do korzystania z KSeF
    /// </summary>
    /// <param name="services">Rozszerzany interfejs</param>
    /// <param name="configure">Opcje klienta KSeF</param>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddKSeFClient(this IServiceCollection services,
        Action<KSeFClientOptions> configure)
    {
        KSeFClientOptions options = new();
        configure(options);
        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new InvalidOperationException($"{nameof(options.BaseUrl)} musi być poprawnym URL.");
        }

        services.AddSingleton(options);

        // UseCamelCaseForRequests is a non-nullable bool with a default of false.
        if (options.UseCamelCaseForRequests)
        {
            JsonUtil.ResetConfigurationForCasePropertyName(true);
        }

        services
            .AddHttpClient<IRestClient, RestClient>(http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl);
                if (options.CustomHeaders != null && options.CustomHeaders.Count > 0)
                {
                    foreach (KeyValuePair<string, string> header in options.CustomHeaders)
                    {
                        http.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                // Domyślny timeout HttpClient (100s) jest zbyt krótki dla operacji wsadowych
                // wysyłających paczki do 100 MB na część (limit API KSeF).
                http.Timeout = TimeSpan.FromMinutes(5);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                HttpClientHandler handler = new();
                if (options.WebProxy != null)
                {
                    handler.Proxy = options.WebProxy;
                    handler.UseProxy = true;
                }
                return handler;
            });

        services.AddSingleton<IRouteBuilder>(sp =>
        {
            return new RouteBuilder(options.ApiConfiguration.ApiVersion);
        });

        services.AddScoped<ISearchPermissionClient, SearchPermissionClient>();
        services.AddScoped<IKSeFClient, KSeFClient>();
        services.AddScoped<ITestDataClient, TestDataClient>();
        services.AddScoped<IAuthCoordinator, AuthCoordinator>();
        services.AddScoped<ILimitsClient, LimitsClient>();
        services.AddScoped<IActiveSessionsClient, ActiveSessionsClient>();
        services.AddScoped<IAuthorizationClient, AuthorizationClient>();

        services.AddSingleton<IPersonTokenService, PersonTokenService>();
        services.AddScoped<IVerificationLinkService, VerificationLinkService>();

        if(!string.IsNullOrEmpty( options.ResourcesPath))
        {
            services.AddLocalization(localizationOptions =>
            {
                localizationOptions.ResourcesPath = options.ResourcesPath;
            });

            if (!string.IsNullOrEmpty(options.DefaultCulture))
            {
                services.Configure<RequestLocalizationOptions>(localizationOptions =>
                {
                    localizationOptions.SetDefaultCulture(options.DefaultCulture);

                    if (options.SupportedCultures != null && options.SupportedCultures.Length > 0)
                    {
                        localizationOptions.AddSupportedCultures(options.SupportedCultures);
                    }

                    if (options.SupportedUICultures != null && options.SupportedUICultures.Length > 0)
                    {
                        localizationOptions.AddSupportedUICultures(options.SupportedUICultures);
                    }
                });
            }
        }

        return services;
    }

    /// <summary>
    /// Rejestruje klienta Latarni.
    /// </summary>
    public static IServiceCollection AddLighthouseClient(this IServiceCollection services,
        Action<LighthouseClientOptions> configure)
    {
        Guard.ThrowIfNull(services);
        Guard.ThrowIfNull(configure);

        LighthouseClientOptions options = new();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            // Domyślnie w całym SDK preferujemy TEST, jeśli nie wskazano inaczej.
            options.BaseUrl = LighthouseEnvironmentsUris.TEST;
        }

        services.Replace(ServiceDescriptor.Singleton(options));
        services.AddScoped<ILighthouseClient, LighthouseClient>();
        return services;
    }

    /// <summary>
    /// Rejestruje klienta Latarni z podanym adresem bazowym.
    /// </summary>
    public static IServiceCollection AddLighthouseClient(this IServiceCollection services, string baseUrl)
        => services.AddLighthouseClient(options => options.BaseUrl = baseUrl);

    /// <summary>
    /// Rejestruje wszystkie potrzebne serwisy do korzystania z klienta kryptograficznego,
    /// używając domyślnego mechanizmu pobierania certyfikatów.
    /// </summary>
    /// <param name="services">Rozszerzany interfejs.</param>
    /// <param name="warmupMode">Tryb "rozgrzewania" usługi. Domyślnie: Blocking.</param>
    public static IServiceCollection AddCryptographyClient(this IServiceCollection services,
        CryptographyServiceWarmupMode warmupMode = CryptographyServiceWarmupMode.Blocking)
    {
        // 1. Rejestracja klienta kryptograficznego jako singleton, jeśli jeszcze nie istnieje
        services.TryAddSingleton<ICryptographyClient, CryptographyClient>();

        // 2. Rejestracja domyślnego fetchera (zależnego od ICryptographyClient),
        // Jeśli zarejestrowano już własną implementację ICertificateFetcher, ta rejestracja zostanie pominięta.
        services.TryAddSingleton<ICertificateFetcher, DefaultCertificateFetcher>();

        // Rejestracja głównej usługi kryptograficznej (DI dostarczy jej ICertificateFetcher).
        services.AddSingleton<ICryptographyService, CryptographyService>();

        // 3. Rejestracja usługi hostowanej (Hosted Service) z wybranym trybem startu
        services.AddHostedService(provider =>
        {
            ICryptographyService cryptoService = provider.GetRequiredService<ICryptographyService>();
            return new CryptographyWarmupHostedService(cryptoService, warmupMode);
        });

        // 4. Inicjalizacja konfiguracji kryptograficznej (rejestracja algorytmu ECdsa)
        CryptographyConfigInitializer.EnsureInitialized();
        return services;
    }

    /// <summary>
    /// Rejestruje serwisy klienta kryptograficznego z użyciem niestandardowego delegata.
    /// </summary>
    /// <remarks>
    /// Ta metoda jest przestarzała. Zalecanym podejściem jest stworzenie własnej klasy
    /// implementującą ICertificateFetcher, zarejestrowanie jej w kontenerze DI
    /// i wywołanie przeciążenia AddCryptographyClient() bez parametru delegata.
    /// </remarks> 
    /// <param name="services">Rozszerzany interfejs</param>
    /// <param name="warmupMode">Tryb "rozgrzewania" usługi. Domyślnie: Blocking.</param>
    /// <param name="pemCertificatesFetcher">Delegat służący do pobrania publicznych certyfikatów KSeF</param>
    /// <exception cref="ArgumentException"></exception>
    [Obsolete("Ta metoda jest przestarzała. Zamiast niej utwórz klasę implementującą ICertificateFetcher i zarejestruj ją w kontenerze DI.")]
    public static IServiceCollection AddCryptographyClient(this IServiceCollection services,
        Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> pemCertificatesFetcher,
        CryptographyServiceWarmupMode warmupMode = CryptographyServiceWarmupMode.Blocking)
    {
        Guard.ThrowIfNull(pemCertificatesFetcher);

        AddCryptographyClient(services, warmupMode);

        // Nadpisuje rejestrację ICertificateFetcher dostarczając implementację,
        // która opakowuje stary delegat. AddScoped (a nie TryAdd),
        // aby ewentualne poprzednie zarejestrowanie zostało nadpisane.
        services.AddScoped<ICertificateFetcher>(provider =>
        {
            return new FuncCertificateFetcherAdapter(pemCertificatesFetcher);
        });

        return services;
    }

    internal class FuncCertificateFetcherAdapter(Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> fetcher) : ICertificateFetcher
    {
        private readonly Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> _fetcher = fetcher;

        public Task<ICollection<PemCertificateInfo>> GetCertificatesAsync(CancellationToken cancellationToken) => _fetcher(cancellationToken);
    }
}
