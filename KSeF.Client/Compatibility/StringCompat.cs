#if NETSTANDARD2_0
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfillowe metody rozszerzające dla <see cref="string"/> na netstandard2.0.
/// Zapewnia przeciążenie <c>string.Contains(string, StringComparison)</c> dostępne od .NET Core 2.1.
/// </summary>
internal static class StringCompat
{
    /// <summary>
    /// Zwraca wartość wskazującą, czy podany podciąg występuje w tym ciągu,
    /// używając określonego typu porównania.
    /// </summary>
    /// <param name="source">Ciąg źródłowy do przeszukania.</param>
    /// <param name="value">Szukany ciąg.</param>
    /// <param name="comparison">Typ porównania ciągów.</param>
    /// <returns><c>true</c> jeśli wartość występuje w tym ciągu; w przeciwnym razie <c>false</c>.</returns>
    public static bool Contains(this string source, string value, StringComparison comparison)
    {
        return source.IndexOf(value, comparison) >= 0;
    }
}
#endif
