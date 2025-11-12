using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;

namespace KSeF.Client.Tests.Core.E2E;
public abstract class TestBase : IDisposable
{
    internal const int SleepTime = 2000;

    protected IServiceScope _scope = default!;
    private ServiceProvider _serviceProvider = default!;

    protected static readonly CancellationToken CancellationToken = CancellationToken.None;
    protected IKSeFClient KsefClient => _scope.ServiceProvider.GetRequiredService<IKSeFClient>();
    protected IAuthorizationClient AuthorizationClient => _scope.ServiceProvider.GetRequiredService<IAuthorizationClient>();
    protected IActiveSessionsClient ActiveSessionsClient => _scope.ServiceProvider.GetRequiredService<IActiveSessionsClient>();
    protected ILimitsClient LimitsClient => _scope.ServiceProvider.GetRequiredService<ILimitsClient>();
    protected ITestDataClient TestDataClient => _scope.ServiceProvider.GetRequiredService<ITestDataClient>();

    protected ISignatureService SignatureService => _scope.ServiceProvider.GetRequiredService<ISignatureService>();
    protected IPersonTokenService TokenService => _scope.ServiceProvider.GetRequiredService<IPersonTokenService>();
    protected ICryptographyService CryptographyService => _scope.ServiceProvider.GetRequiredService<ICryptographyService>();


    public TestBase()
    {
        CryptoConfig.AddAlgorithm(
            typeof(Ecdsa256SignatureDescription),
                "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256");

        ServiceCollection services = new ServiceCollection();

        ApiSettings apiSettings = TestConfig.GetApiSettings();

        string? customHeadersFromSettings = TestConfig.Load()["ApiSettings:customHeaders"];
        if (!string.IsNullOrEmpty(customHeadersFromSettings))
        {
            apiSettings.CustomHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(customHeadersFromSettings);
        }

        services.AddKSeFClient(options =>
        {
            options.BaseUrl = apiSettings.BaseUrl!;
            options.CustomHeaders = apiSettings.CustomHeaders ?? new Dictionary<string, string>();
        });

        // UWAGA! w testach nie używamy AddCryptographyClient tylko rejestrujemy ręcznie, bo on uruchamia HostedService w tle
        services.AddSingleton<ICryptographyClient, CryptographyClient>();
        services.AddSingleton<ICertificateFetcher, DefaultCertificateFetcher>();
        services.AddSingleton<ICryptographyService, CryptographyService>();
        // Rejestracja usługi hostowanej (Hosted Service) jako singleton na potrzeby testów
        services.AddSingleton<CryptographyWarmupHostedService>();

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        _scope = _serviceProvider.CreateScope();

        // opcjonalne: inicjalizacja lub inne czynności startowe
        // Uruchomienie usługi hostowanej w trybie blokującym (domyślnym) na potrzeby testów
        _scope.ServiceProvider.GetRequiredService<CryptographyWarmupHostedService>()
                   .StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider?.Dispose();
    }
}
