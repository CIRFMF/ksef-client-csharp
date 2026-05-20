using System.ComponentModel.DataAnnotations;
using System.Net;

namespace KSeF.Client.DI;

/// <summary>
/// Opcje konfiguracyjne klienta KSeF.
/// </summary>
public class KSeFClientOptions
{
    [Required(ErrorMessage = "Pole BaseUrl jest wymagane.")]
    [Url(ErrorMessage = "Pole BaseUrl musi być poprawnym adresem URL.")]
    public string BaseUrl { get; set; } = "";

    [Required(ErrorMessage = "Pole BaseQRUrl jest wymagane.")]
    [Url(ErrorMessage = "Pole BaseQRUrl musi być poprawnym adresem URL.")]
    public string BaseQRUrl { get; set; } = "";

    /// <summary>
    /// Opcjonalny bazowy adres URL usługi Latarni.
    /// Jeśli nie ustawiony, używany jest domyślny adres dla środowiska PROD.
    /// </summary>
    [Url(ErrorMessage = "Pole LighthouseBaseUrl musi być poprawnym adresem URL.")]
    public string LighthouseBaseUrl { get; set; } = "";

    public Dictionary<string, string> CustomHeaders { get; set; }
    public IWebProxy WebProxy { get; set; }

    public string ResourcesPath { get; set; }
    public string[] SupportedUICultures { get; set; }
    public string[] SupportedCultures { get; set; }
    public string DefaultCulture { get; set; }

    public ApiConfiguration ApiConfiguration { get; set; } = new ApiConfiguration();

    /// <summary>
    /// Jeśli ustawione na true, serializacja JSON dla wychodzących żądań do API
    /// będzie używać camelCase dla nazw właściwości. Domyślnie false.
    /// </summary>
    public bool UseCamelCaseForRequests { get; set; } = false;

    /// <summary>
    /// Jeśli ustawione na true (domyślnie), klient HTTP będzie preferował HTTP/2
    /// z automatycznym fallbackiem do HTTP/1.1 (<see cref="HttpVersionPolicy.RequestVersionOrLower"/>),
    /// gdy serwer lub infrastruktura pośrednia (proxy, load balancer) nie obsługuje HTTP/2.
    /// Ustaw na false, aby wymusić wyłącznie HTTP/1.1 — np. gdy proxy
    /// nie obsługuje HTTP/2 i nie wykonuje poprawnej negocjacji.
    /// </summary>
    /// <remarks>
    /// Właściwość jest uwzględniana wyłącznie na platformach .NET 5+.
    /// Na .NET Standard 2.0 ustawienie jest ignorowane (HttpClient nie udostępnia tych API).
    /// </remarks>
    public bool UseHttp2 { get; set; } = true;

    /// <summary>
    /// Konfiguracja Circuit Breakera dla wywołań HTTP do KSeF.
    /// Domyślnie mechanizm jest włączony.
    /// </summary>
    public KsefCircuitBreakerOptions CircuitBreaker { get; set; } = new KsefCircuitBreakerOptions();
}