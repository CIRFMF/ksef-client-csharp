#if NETSTANDARD2_0
namespace System.Text;

/// <summary>
/// Polyfill for <c>System.Text.CompositeFormat</c> available since .NET 8.
/// On netstandard2.0, wraps the format string for use with <c>string.Format()</c>.
/// </summary>
/// <remarks>
/// The real .NET 8+ <c>CompositeFormat</c> pre-parses the format string for performance.
/// This polyfill simply stores the raw format string and delegates to <c>string.Format()</c>.
/// The overload <c>string.Format(IFormatProvider, CompositeFormat, ...)</c> does not exist
/// on netstandard2.0, so the polyfill provides an implicit conversion to <c>string</c>.
/// </remarks>
internal sealed class CompositeFormat
{
    private readonly string _format;

    private CompositeFormat(string format)
    {
        _format = format;
    }

    /// <summary>
    /// Parses a format string into a <see cref="CompositeFormat"/> instance.
    /// </summary>
    /// <param name="format">The format string to parse.</param>
    /// <returns>A <see cref="CompositeFormat"/> wrapping the format string.</returns>
    public static CompositeFormat Parse(string format) => new CompositeFormat(format);

    /// <summary>
    /// Implicit conversion to <see cref="string"/> for use with <c>string.Format()</c>.
    /// </summary>
    public static implicit operator string(CompositeFormat cf) => cf._format;

    /// <inheritdoc/>
    public override string ToString() => _format;
}
#endif
