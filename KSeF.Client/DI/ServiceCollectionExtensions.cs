using System.Net.Http.Headers;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeFClient.Api.Services;
using KSeFClient.Core.Interfaces;
using KSeFClient.Http;
using Microsoft.Extensions.DependencyInjection;
namespace KSeFClient.DI;

/// <summary>
/// Extension methods do rejestracji KSeF SDK w kontenerze DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje wszystkie potrzebne serwisy do korzystania z KSeF
    /// </summary>
    public static IServiceCollection AddKSeFClient(this IServiceCollection services, Action<KSeFClientOptions> configure)
    {
        var options = new KSeFClientOptions();
        configure(options);
        if (string.IsNullOrEmpty(options.BaseUrl))
            throw new ArgumentException("BaseUrl must be a valid URL.", nameof(options.BaseUrl));

        services.AddSingleton(options);

        services
            .AddHttpClient<IRestClient, RestClient>(http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl);
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

        services.AddScoped<IKSeFClient, Http.KSeFClient>();
        services.AddScoped<IAuthCoordinator, AuthCoordinator>();
        services.AddScoped<ICryptographyService, CryptographyService>();
        services.AddScoped<ISignatureService, SignatureService>();

        return services;
    }
}
