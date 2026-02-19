#nullable enable
#if NETFRAMEWORK
namespace KSeF.Client.Tests.Core.Compatibility;

/// <summary>
/// Polyfille generycznych metod <see cref="Enum"/> niedostępnych na .NET Framework 4.8:
/// <c>Enum.GetValues&lt;T&gt;()</c> i <c>Enum.Parse&lt;T&gt;(string)</c> (dodane w .NET 5).
/// </summary>
internal static class EnumPolyfills
{
    /// <summary>
    /// Zwraca tablicę wartości enumeracji typu <typeparamref name="T"/>.
    /// Polyfill dla <c>Enum.GetValues&lt;T&gt;()</c>.
    /// </summary>
    public static T[] GetValues<T>() where T : struct, Enum
        => (T[])Enum.GetValues(typeof(T));

    /// <summary>
    /// Parsuje ciąg znaków na wartość enumeracji typu <typeparamref name="T"/>.
    /// Polyfill dla <c>Enum.Parse&lt;T&gt;(string)</c>.
    /// </summary>
    public static T Parse<T>(string value) where T : struct, Enum
        => (T)Enum.Parse(typeof(T), value);
}
#endif
