#nullable enable
using System.Globalization;

namespace KSeF.Client.Validation;

/// <summary>
/// Waliduje numery KSeF w formacie 35- i 36-znakowym.
/// </summary>
public static class KsefNumberValidator
{
    private static readonly DateTime KsefV36CutoffDate = new(2024, 2, 1);

    /// <summary>
    /// Sprawdza format i sumę kontrolną numeru 35-znakowego lub format i datę numeru 36-znakowego.
    /// </summary>
    /// <param name="number">Numer KSeF do walidacji.</param>
    /// <returns><see langword="true"/>, jeśli numer spełnia reguły danego formatu.</returns>
    /// <remarks>
    /// Dla numerów 36-znakowych wymagana jest data wcześniejsza niż 1 lutego 2024 r.
    /// </remarks>
    public static bool IsValid(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return false;
        }

        if (RegexPatterns.KsefNumberV35.IsMatch(number))
        {
            return KSeF.Client.Core.KsefNumberValidator.IsValid(number, out _);
        }

        // Walidacja numerów KSeF 1.0 w formacie 36-znakowym.
        if (!RegexPatterns.KsefNumberV36.IsMatch(number))
        {
            return false;
        }

        string datePart = number.Substring(11, 8);

        return DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            && date < KsefV36CutoffDate;
    }
}
