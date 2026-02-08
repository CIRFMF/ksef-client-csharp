#nullable enable
using Microsoft.Extensions.Configuration;

namespace KSeF.Client.Tests.Core.Config;
public static class TestConfig
{
    private static IConfigurationRoot configuration = 
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .AddUserSecrets(typeof(TestConfig).Assembly, optional: true)
            .Build();

    /// <summary>
    /// Wczytuje konfigurację aplikacji z predefiniowanego źródła konfiguracji.
    /// </summary>
    /// <remarks>Ta metoda pobiera konfigurację z użyciem predefiniowanego źródła i mechanizmu buforowania. 
    /// Kolejne wywołania zwracają tę samą instancję konfiguracji.</remarks>
    /// <returns>Instancja <see cref="IConfigurationRoot"/> reprezentująca wczytaną konfigurację.</returns>
    public static IConfigurationRoot Load() => configuration;

    /// <summary>
    /// Pobiera ustawienia API z konfiguracji aplikacji.
    /// </summary>
    /// <remarks>Ta metoda wczytuje konfigurację i pobiera ustawienia zdefiniowane w sekcji "ApiSettings".
    /// Jeśli sekcja "ApiSettings" nie zostanie znaleziona lub jest pusta, zostanie zwrócona nowa instancja <see
    /// cref="ApiSettings"/>.</remarks>
    /// <returns>Instancja <see cref="ApiSettings"/> zawierająca ustawienia konfiguracji API.</returns>
    public static ApiSettings GetApiSettings()
    {
        IConfigurationRoot configuration = Load();
        ApiSettings settings = configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new();

        return settings;
    }
}