#nullable enable
#if NETFRAMEWORK
namespace KSeF.Client.Tests.Utils.Compatibility;

/// <summary>
/// Polyfill dla string.Contains(string, StringComparison) niedostępnego na .NET Framework 4.8.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Sprawdza, czy ciąg zawiera podciąg z uwzględnieniem podanego trybu porównania.
    /// </summary>
    public static bool Contains(this string s, string value, StringComparison comparisonType)
        => s.IndexOf(value, comparisonType) >= 0;
}
#endif
