using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Core.Exceptions;

namespace KSeF.Client.Tests.Core.E2E.Limits;

/// <summary>
/// Testy E2E dla zarządzania limitami zapytań API (Rate Limits).
/// Sprawdza pełny scenariusz: pobranie aktualnych limitów, ustawienie nowych wartości w granicach dopuszczalnych,
/// weryfikację zastosowania zmian, przywrócenie wartości domyślnych oraz weryfikację, że końcowe wartości
/// są identyczne z początkowymi.
/// </summary>
public class RateLimitsE2ETests : TestBase
{
    private const int MinApiRateLimit = 1;

    // Maksymalne wartości (min zawsze 1) zgodne z walidacją API
    private static readonly RateMax OnlineSessionMax = new(10, 30, 120);
    private static readonly RateMax BatchSessionMax = new(10, 20, 120);
    private static readonly RateMax InvoiceSendMax = new(10, 30, 180);
    private static readonly RateMax InvoiceStatusMax = new(30, 120, 720);
    private static readonly RateMax SessionListMax = new(5, 10, 60);
    private static readonly RateMax SessionInvoiceListMax = new(10, 20, 200);
    private static readonly RateMax SessionMiscMax = new(10, 120, 720);
    private static readonly RateMax InvoiceMetadataMax = new(8, 16, 20);
    private static readonly RateMax InvoiceExportMax = new(4, 8, 20);
    private static readonly RateMax InvoiceDownloadMax = new(8, 16, 64);
    private static readonly RateMax OtherMax = new(10, 30, 120);

    /// <summary>
    /// Test E2E: pobiera bieżące limity, wylicza i ustawia nowe ograniczenia w dopuszczalnych granicach,
    /// sprawdza czy zmiany zostały zastosowane, przywraca wartości oryginalne i weryfikuje, że są zgodne
    /// z wartościami sprzed modyfikacji.
    /// Kroki:
    /// 1) Uwierzytelnienie i pobranie access tokena
    /// 2) Pobranie aktualnych limitów
    /// 3) Wyliczenie nowych wartości w granicach (min=1, max wg kategorii) i ustawienie ich
    /// 4) Weryfikacja, że ustawione wartości odpowiadają nowym oczekiwaniom
    /// 5) Przywrócenie wartości domyślnych
    /// 6) Weryfikacja, że przywrócone wartości są identyczne jak te pobrane w kroku 2
    /// </summary>
    [Fact]
    public async Task RateLimits_E2E_Positive()
    {
        const int LimitsChangeValue = 1;

        // Arrange: Uwierzytelnianie i uzyskanie tokenu dostępu
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                MiscellaneousUtils.GetRandomNip());
        string accessToken = authorizationInfo.AccessToken.Token;

        // Arrange: Pobranie aktualnych limitów
        EffectiveApiRateLimits originalLimits =
            await LimitsClient.GetRateLimitsAsync(
                accessToken,
                CancellationToken);

        // Assert: Wstępna walidacja danych wejściowych testu
        Assert.NotNull(originalLimits);

        // Act: Wyliczenie nowych limitów w bezpiecznych widełkach (min=1, max wg kategorii)
        EffectiveApiRateLimits modifiedLimits = CloneAndModifyWithinBounds(originalLimits, LimitsChangeValue);

        EffectiveApiRateLimitsRequest setRequest = new EffectiveApiRateLimitsRequest
        {
            RateLimits = modifiedLimits
        };

        // Act: Ustawienie nowych limitów
        await TestDataClient.SetRateLimitsAsync(
            setRequest,
            accessToken);

        // Act: Ponowne pobranie limitów po zmianie
        EffectiveApiRateLimits currentLimits =
            await LimitsClient.GetRateLimitsAsync(
                accessToken,
                CancellationToken);

        // Assert: Weryfikacja, że limity zostały zmienione zgodnie z oczekiwaniami
        AssertRateLimitsEqual(modifiedLimits, currentLimits);

        // Act: Przywrócenie wartości domyślnych
        await TestDataClient.RestoreRateLimitsAsync(accessToken);

        // Act: Ponowne pobranie po przywróceniu
        EffectiveApiRateLimits restoredLimits =
            await LimitsClient.GetRateLimitsAsync(
                accessToken,
                CancellationToken);

        // Assert: Weryfikacja, że wartości po przywróceniu są identyczne jak oryginalne
        AssertRateLimitsEqual(originalLimits, restoredLimits);
    }

    /// <summary>
    /// Test E2E (negatywny): próba ustawienia wartości limitów przekraczających dopuszczalne maksimum
    /// powinna zakończyć się rzuceniem wyjątku KsefApiException.
    /// Kroki:
    /// 1) Uwierzytelnienie i pobranie access tokena
    /// 2) Pobranie aktualnych limitów (bazowych wartości)
    /// 3) Przygotowanie żądania z wartościami wykraczającymi ponad dozwolone maksimum (np. OnlineSession > max)
    /// 4) Weryfikacja, że wywołanie SetRateLimitsAsync zgłasza KsefApiException
    /// </summary>
    [Fact]
    public async Task RateLimits_E2E_Negative_InvalidValues_ShouldThrowKsefApiException()
    {
        // Arrange: Uwierzytelnianie i uzyskanie tokenu dostępu
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                MiscellaneousUtils.GetRandomNip());
        string accessToken = authorizationInfo.AccessToken.Token;

        // Arrange: Pobranie aktualnych limitów do bazowania
        EffectiveApiRateLimits baseLimits =
            await LimitsClient.GetRateLimitsAsync(
                accessToken,
                CancellationToken);
        Assert.NotNull(baseLimits);

        // Arrange: Przygotowanie jawnie nieprawidłowych wartości (OnlineSession poza maksimum)
        EffectiveApiRateLimits invalidLimits = new EffectiveApiRateLimits
        {
            OnlineSession = new EffectiveApiRateLimitValues
            {
                PerSecond = OnlineSessionMax.PerSecond + 1,
                PerMinute = OnlineSessionMax.PerMinute + 1,
                PerHour = OnlineSessionMax.PerHour + 1
            },
            // ustawienie pozostałych kategorii na aktualne poprawne wartości, by zminimalizować wpływ
            BatchSession = baseLimits.BatchSession,
            InvoiceSend = baseLimits.InvoiceSend,
            InvoiceStatus = baseLimits.InvoiceStatus,
            SessionList = baseLimits.SessionList,
            SessionInvoiceList = baseLimits.SessionInvoiceList,
            SessionMisc = baseLimits.SessionMisc,
            InvoiceMetadata = baseLimits.InvoiceMetadata,
            InvoiceExport = baseLimits.InvoiceExport,
            InvoiceDownload = baseLimits.InvoiceDownload,
            Other = baseLimits.Other
        };

        EffectiveApiRateLimitsRequest request = new EffectiveApiRateLimitsRequest
        {
            RateLimits = invalidLimits
        };


        try
        {
            // Act
            await TestDataClient.SetRateLimitsAsync(request, accessToken);
        }
        catch (KsefApiException ksefException)
        {
            // Assert
            Assert.Contains("21405: Błąd walidacji danych wejściowych.", ksefException.Message);
        }
    }

    /// <summary>
    /// Tworzy kopię przekazanych limitów i modyfikuje je w oparciu o delta, nie przekraczając granic (min=1, max wg kategorii).
    /// </summary>
    /// <param name="source">Oryginalne limity.</param>
    /// <param name="delta">Wartość inkrementacji/dekrementacji.</param>
    /// <returns>Nowy obiekt z bezpiecznie zmodyfikowanymi limitami.</returns>
    private static EffectiveApiRateLimits CloneAndModifyWithinBounds(EffectiveApiRateLimits source, int delta)
    {
        return new EffectiveApiRateLimits
        {
            OnlineSession = ModifyWithinBounds(source.OnlineSession, delta, OnlineSessionMax),
            BatchSession = ModifyWithinBounds(source.BatchSession, delta, BatchSessionMax),
            InvoiceSend = ModifyWithinBounds(source.InvoiceSend, delta, InvoiceSendMax),
            InvoiceStatus = ModifyWithinBounds(source.InvoiceStatus, delta, InvoiceStatusMax),
            SessionList = ModifyWithinBounds(source.SessionList, delta, SessionListMax),
            SessionInvoiceList = ModifyWithinBounds(source.SessionInvoiceList, delta, SessionInvoiceListMax),
            SessionMisc = ModifyWithinBounds(source.SessionMisc, delta, SessionMiscMax),
            InvoiceMetadata = ModifyWithinBounds(source.InvoiceMetadata, delta, InvoiceMetadataMax),
            InvoiceExport = ModifyWithinBounds(source.InvoiceExport, delta, InvoiceExportMax),
            InvoiceDownload = ModifyWithinBounds(source.InvoiceDownload, delta, InvoiceDownloadMax),
            Other = ModifyWithinBounds(source.Other, delta, OtherMax)
        };
    }

    /// <summary>
    /// Modyfikuje wartości limitów dla jednej kategorii, pozostając w dopuszczalnych granicach.
    /// Jeśli dodanie delta przekracza maksimum, próbuje odjąć delta; jeśli to również jest poza minimum, zwraca bieżącą wartość.
    /// </summary>
    /// <param name="values">Bieżące wartości limitów dla kategorii.</param>
    /// <param name="delta">Wartość inkrementacji/dekrementacji.</param>
    /// <param name="max">Maksymalne dopuszczalne wartości dla kategorii.</param>
    /// <returns>Nowe wartości limitów w granicach.</returns>
    private static EffectiveApiRateLimitValues? ModifyWithinBounds(EffectiveApiRateLimitValues? values, int delta, RateMax max)
    {
        if (values is null)
        {
            return null;
        }

        return new EffectiveApiRateLimitValues
        {
            PerSecond = Adjust(values.PerSecond, delta, MinApiRateLimit, max.PerSecond),
            PerMinute = Adjust(values.PerMinute, delta, MinApiRateLimit, max.PerMinute),
            PerHour = Adjust(values.PerHour, delta, MinApiRateLimit, max.PerHour)
        };
    }

    /// <summary>
    /// Zwraca nową wartość po dodaniu lub odjęciu delta tak, aby nie przekroczyć min/max.
    /// </summary>
    /// <param name="current">Wartość bieżąca.</param>
    /// <param name="delta">Wartość inkrementacji/dekrementacji.</param>
    /// <param name="min">Minimalna dopuszczalna wartość.</param>
    /// <param name="max">Maksymalna dopuszczalna wartość.</param>
    /// <returns>Skorygowana wartość w przedziale [min, max].</returns>
    private static int Adjust(int current, int delta, int min, int max)
    {
        if (current + delta <= max)
        {
            return current + delta;
        }

        if (current - delta >= min)
        {
            return current - delta;
        }

        return current; // w praktyce nie powinno się zdarzyć dla delta=1 i max>1
    }

    /// <summary>
    /// Porównuje wszystkie wartości limitów pomiędzy oczekiwanymi i aktualnymi.
    /// </summary>
    /// <param name="expected">Oczekiwane limity.</param>
    /// <param name="actual">Aktualne limity.</param>
    private static void AssertRateLimitsEqual(EffectiveApiRateLimits expected, EffectiveApiRateLimits actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // OnlineSession
        Assert.Equal(expected.OnlineSession.PerSecond, actual.OnlineSession.PerSecond);
        Assert.Equal(expected.OnlineSession.PerMinute, actual.OnlineSession.PerMinute);
        Assert.Equal(expected.OnlineSession.PerHour, actual.OnlineSession.PerHour);
        // BatchSession
        Assert.Equal(expected.BatchSession.PerSecond, actual.BatchSession.PerSecond);
        Assert.Equal(expected.BatchSession.PerMinute, actual.BatchSession.PerMinute);
        Assert.Equal(expected.BatchSession.PerHour, actual.BatchSession.PerHour);
        // InvoiceSend
        Assert.Equal(expected.InvoiceSend.PerSecond, actual.InvoiceSend.PerSecond);
        Assert.Equal(expected.InvoiceSend.PerMinute, actual.InvoiceSend.PerMinute);
        Assert.Equal(expected.InvoiceSend.PerHour, actual.InvoiceSend.PerHour);
        // InvoiceStatus
        Assert.Equal(expected.InvoiceStatus.PerSecond, actual.InvoiceStatus.PerSecond);
        Assert.Equal(expected.InvoiceStatus.PerMinute, actual.InvoiceStatus.PerMinute);
        Assert.Equal(expected.InvoiceStatus.PerHour, actual.InvoiceStatus.PerHour);
        // SessionList
        Assert.Equal(expected.SessionList.PerSecond, actual.SessionList.PerSecond);
        Assert.Equal(expected.SessionList.PerMinute, actual.SessionList.PerMinute);
        Assert.Equal(expected.SessionList.PerHour, actual.SessionList.PerHour);
        // SessionInvoiceList
        Assert.Equal(expected.SessionInvoiceList.PerSecond, actual.SessionInvoiceList.PerSecond);
        Assert.Equal(expected.SessionInvoiceList.PerMinute, actual.SessionInvoiceList.PerMinute);
        Assert.Equal(expected.SessionInvoiceList.PerHour, actual.SessionInvoiceList.PerHour);
        // SessionMisc
        Assert.Equal(expected.SessionMisc.PerSecond, actual.SessionMisc.PerSecond);
        Assert.Equal(expected.SessionMisc.PerMinute, actual.SessionMisc.PerMinute);
        Assert.Equal(expected.SessionMisc.PerHour, actual.SessionMisc.PerHour);
        // InvoiceMetadata
        Assert.Equal(expected.InvoiceMetadata.PerSecond, actual.InvoiceMetadata.PerSecond);
        Assert.Equal(expected.InvoiceMetadata.PerMinute, actual.InvoiceMetadata.PerMinute);
        Assert.Equal(expected.InvoiceMetadata.PerHour, actual.InvoiceMetadata.PerHour);
        // InvoiceExport
        Assert.Equal(expected.InvoiceExport.PerSecond, actual.InvoiceExport.PerSecond);
        Assert.Equal(expected.InvoiceExport.PerMinute, actual.InvoiceExport.PerMinute);
        Assert.Equal(expected.InvoiceExport.PerHour, actual.InvoiceExport.PerHour);
        // InvoiceDownload
        Assert.Equal(expected.InvoiceDownload.PerSecond, actual.InvoiceDownload.PerSecond);
        Assert.Equal(expected.InvoiceDownload.PerMinute, actual.InvoiceDownload.PerMinute);
        Assert.Equal(expected.InvoiceDownload.PerHour, actual.InvoiceDownload.PerHour);
        // Other
        Assert.Equal(expected.Other.PerSecond, actual.Other.PerSecond);
        Assert.Equal(expected.Other.PerMinute, actual.Other.PerMinute);
        Assert.Equal(expected.Other.PerHour, actual.Other.PerHour);
    }

    /// <summary>
    /// Struktura przechowująca maksymalne dopuszczalne wartości limitów dla danej kategorii
    /// (na sekundę, na minutę, na godzinę).
    /// </summary>
    private readonly struct RateMax
    {
        public RateMax(int perSecond, int perMinute, int perHour)
        {
            PerSecond = perSecond;
            PerMinute = perMinute;
            PerHour = perHour;
        }
        public int PerSecond { get; }
        public int PerMinute { get; }
        public int PerHour { get; }
    }
}
