using System;

namespace KSeF.Client.Tests.Utils;

/// <summary>
/// Helper do obliczania dat w polskiej strefie czasowej (Europe/Warsaw, CET/CEST).
/// <para>
/// KSeF waliduje daty dokumentów (P_1, DataWytworzeniaFa) w polskiej strefie czasowej.
/// Użycie <c>DateTime.UtcNow.Date</c> lub <c>DateTime.Today</c> prowadzi do niestabilnych
/// testów w oknie ~2h wokół północy CET, gdy data UTC różni się od daty CET.
/// </para>
/// </summary>
public static class KsefDateTimeHelper
{
    /// <summary>
    /// Polska strefa czasowa (Europe/Warsaw) z fallbackiem na identyfikator Windows.
    /// </summary>
    private static readonly Lazy<TimeZoneInfo> WarsawTimeZone = new(() =>
    {
        // Próba użycia identyfikatora IANA (Linux, macOS, .NET 6+, Mono)
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"); }
        catch (TimeZoneNotFoundException) { }

        // Fallback: identyfikator Windows (.NET Framework 4.8 na Windows)
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
        catch (TimeZoneNotFoundException) { }

        throw new TimeZoneNotFoundException(
            "Nie znaleziono polskiej strefy czasowej. " +
            "Sprawdź, czy system obsługuje 'Europe/Warsaw' lub 'Central European Standard Time'.");
    });

    /// <summary>
    /// Bieżąca data w polskiej strefie czasowej (bez komponentu czasu).
    /// </summary>
    public static DateTime GetWarsawToday() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WarsawTimeZone.Value).Date;

    /// <summary>
    /// Jutrzejsza data w polskiej strefie czasowej.
    /// </summary>
    public static DateTime GetWarsawTomorrow() => GetWarsawToday().AddDays(1);

    /// <summary>
    /// Bieżąca data i czas w polskiej strefie czasowej.
    /// </summary>
    public static DateTime GetWarsawNow() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WarsawTimeZone.Value);
}
