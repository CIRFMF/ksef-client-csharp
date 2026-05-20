namespace KSeF.Client.DI;

/// <summary>
/// Konfiguracja Circuit Breakera dla wywołań HTTP do KSeF.
/// </summary>
public sealed class KsefCircuitBreakerOptions
{
    /// <summary>
    /// Włącza lub wyłącza mechanizm Circuit Breakera.
    /// Domyślnie: <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Liczba kolejnych błędów przejściowych, po której obwód zostanie otwarty.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Czas (w sekundach), przez jaki obwód pozostaje otwarty.
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 30;
}
