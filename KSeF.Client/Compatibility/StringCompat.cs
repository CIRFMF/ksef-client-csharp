#if NETSTANDARD2_0
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill extension methods for <see cref="string"/> on netstandard2.0.
/// Provides <c>string.Contains(string, StringComparison)</c> overload available since .NET Core 2.1.
/// </summary>
internal static class StringCompat
{
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string,
    /// using the specified comparison type.
    /// </summary>
    /// <param name="source">The source string to search.</param>
    /// <param name="value">The string to seek.</param>
    /// <param name="comparison">The string comparison type.</param>
    /// <returns><c>true</c> if the value occurs within this string; otherwise, <c>false</c>.</returns>
    public static bool Contains(this string source, string value, StringComparison comparison)
    {
        return source.IndexOf(value, comparison) >= 0;
    }
}
#endif
