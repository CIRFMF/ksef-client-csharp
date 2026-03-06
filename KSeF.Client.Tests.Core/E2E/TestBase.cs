using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using KSeF.Client.Extensions;
using KSeF.Client.Tests.Core.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace KSeF.Client.Tests.Core.E2E;
public abstract class TestBase : IDisposable
{
    internal const int SleepTime = 2000;
    private const string LighthouseBaseUrlSettingKey = "LighthouseSettings:BaseUrl";

    private readonly IServiceScope _scope;
    private readonly ServiceProvider _root;

    protected IServiceProvider Services => _scope.ServiceProvider;
    protected T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    private readonly ServiceProvider _serviceProvider = default!;

    protected static readonly CancellationToken CancellationToken = CancellationToken.None;
    protected IKSeFClient KsefClient => Get<IKSeFClient>();
    protected IAuthorizationClient AuthorizationClient => Get<IAuthorizationClient>();
    protected IActiveSessionsClient ActiveSessionsClient => Get<IActiveSessionsClient >();
    protected ILimitsClient LimitsClient => Get<ILimitsClient>();
    protected ITestDataClient TestDataClient => Get<ITestDataClient>();

    protected IPersonTokenService TokenService => Get<IPersonTokenService>();
    protected ICryptographyService CryptographyService => Get<ICryptographyService>();
    protected IRestClient RestClient => Get<IRestClient>();


    public TestBase()
    {
        CryptographyConfigInitializer.EnsureInitialized();
        ServiceCollection services = new();

        _root = services.BuildServiceProvider();
        _scope = _root.CreateScope();

        ApiSettings apiSettings = TestConfig.GetApiSettings();

        string customHeadersFromSettings = TestConfig.Load()["ApiSettings:customHeaders"] ?? string.Empty;
        if (!string.IsNullOrEmpty(customHeadersFromSettings))
        {
            apiSettings.CustomHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(customHeadersFromSettings)
                ?? [];
        }

        string lighthouseBaseUrlFromConfig = TestConfig.Load()[LighthouseBaseUrlSettingKey] ?? string.Empty;
        bool useCamelCaseForRequestsApiSettings = TestConfig.Load().GetValue<bool>("ApiSettings:UseCamelCaseForRequests");

        services.AddKSeFClient(options =>
        {
            options.BaseUrl = apiSettings.BaseUrl!;
            options.CustomHeaders = apiSettings.CustomHeaders ?? [];

            options.UseCamelCaseForRequests = useCamelCaseForRequestsApiSettings;
        });

        services.AddLighthouseClient(options =>
        {
            options.BaseUrl = string.IsNullOrWhiteSpace(lighthouseBaseUrlFromConfig)
                ? LighthouseEnvironmentsUris.TEST
                : lighthouseBaseUrlFromConfig;
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

    public Task DisposeAsync() => Task.Run(() => Dispose());

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
