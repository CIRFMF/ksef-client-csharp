#if NETSTANDARD2_0
namespace System.Text;

/// <summary>
/// Polyfill dla <c>System.Text.CompositeFormat</c> dostępnego od .NET 8.
/// Na netstandard2.0 opakowuje ciąg formatujący do użycia z <c>string.Format()</c>.
/// </summary>
/// <remarks>
/// Prawdziwy <c>CompositeFormat</c> z .NET 8+ wstępnie parsuje ciąg formatujący dla wydajności.
/// Ten polyfill po prostu przechowuje surowy ciąg formatujący i deleguje do <c>string.Format()</c>.
/// Przeciążenie <c>string.Format(IFormatProvider, CompositeFormat, ...)</c> nie istnieje
/// na netstandard2.0, więc polyfill zapewnia niejawną konwersję na <c>string</c>.
/// </remarks>
internal sealed class CompositeFormat
{
    private readonly string _format;

    private CompositeFormat(string format)
    {
        _format = format;
    }

    /// <summary>
    /// Parsuje ciąg formatujący do instancji <see cref="CompositeFormat"/>.
    /// </summary>
    /// <param name="format">Ciąg formatujący do sparsowania.</param>
    /// <returns>Instancja <see cref="CompositeFormat"/> opakowująca ciąg formatujący.</returns>
    public static CompositeFormat Parse(string format) => new CompositeFormat(format);

    /// <summary>
    /// Niejawna konwersja na <see cref="string"/> do użycia z <c>string.Format()</c>.
    /// </summary>
    public static implicit operator string(CompositeFormat cf) => cf._format;

    /// <inheritdoc/>
    public override string ToString() => _format;
}
#endif
