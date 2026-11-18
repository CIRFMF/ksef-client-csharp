#nullable enable
using KSeF.Client.Core;

namespace KSeF.Client.Validation;

/// <summary>
/// Waliduje format i sumę kontrolną numeru identyfikatora zbiorczego.
/// </summary>
public static class CollectiveIdentifierNumberValidator
{
    /// <summary>
    /// Sprawdza format numeru oraz CRC-8 tekstu przed ostatnim myślnikiem.
    /// </summary>
    /// <param name="number">Numer identyfikatora zbiorczego.</param>
    /// <returns><see langword="true"/>, jeśli format i suma kontrolna są poprawne.</returns>
    public static bool IsValid(string? number)
    {
        if (string.IsNullOrWhiteSpace(number) || !RegexPatterns.CollectiveIdentifierNumber.IsMatch(number))
        {
            return false;
        }

        string checksum = number[^2..];
        string checksumInput = number[..^3];

        return Crc8.CalculateChecksum(checksumInput) == checksum;
    }
}
