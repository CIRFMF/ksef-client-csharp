#nullable enable
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.RateLimits;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.DI;
using KSeF.Client.Tests.Core.Config;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Core.Exceptions;

namespace KSeF.Client.Tests.Core.E2E.Limits;

/// <summary>
/// Testy E2E zarządzania limitami zapytań API (Rate Limits).
/// Sprawdza pełny scenariusz: pobranie aktualnych limitów, ustawienie nowych wartości w granicach dopuszczalnych,
/// weryfikację zastosowania zmian, przywrócenie wartości domyślnych oraz weryfikację, że końcowe wartości
/// są identyczne z początkowymi.
/// </summary>
public class RateLimitsE2ETests : TestBase
{
    public RateLimitsE2ETests()
        : base(disableClientSideCircuitBreaker: true)
    {
    }

    private const int MinApiRateLimit = 1;
    private const int LowOtherPerSecondLimit = 1;
    private const int MaxRequestsToReachOtherPerSecondLimit = 10;
    private const int PermissionPropagationMaxAttempts = 30;
    private const int RateLimitChangeMaxAttempts = 3;
    private const int RateLimitChangeVerificationMaxAttempts = 15;
    private const int CleanupRateLimitMaxAttempts = 5;
    private static readonly TimeSpan RateLimitRetryDelay = TimeSpan.FromMilliseconds(1_200);
    private static readonly TimeSpan RateLimitChangeVerificationDelay = TimeSpan.FromMilliseconds(1_200);
    private static readonly TimeSpan PermissionPropagationPollingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RateLimitsStatePollingDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RateLimiterPropagationDelay = TimeSpan.FromMilliseconds(500);
    private const int MaxAttempts = 12;

    // Maksymalne wartości (min zawsze 1) zgodne z walidacją API
    private static readonly RateMax OnlineSessionMax = new(10, 30, 120);
    private static readonly RateMax BatchSessionMax = new(10, 20, 120);
    private static readonly RateMax InvoiceSendMax = new(10, 30, 180);
    private static readonly RateMax InvoiceStatusMax = new(30, 120, 720);
    private static readonly RateMax SessionListMax = new(5, 10, 60);
    private static readonly RateMax SessionInvoiceListMax = new(10, 20, 200);
    private static readonly RateMax SessionMiscMax = new(10, 120, 720);
    private static readonly RateMax InvoiceMetadataMax = new(8, 16, 20);
    private static readonly RateMax InvoiceExportMax = new(8, 16, 20);
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

        EffectiveApiRateLimitsRequest setRequest = new()
        {
            RateLimits = modifiedLimits
        };

		// Act: Ustawienie nowych limitów
		await TestDataClient.SetRateLimitsAsync(
			setRequest,
			accessToken);

		// Act: Ponowne pobranie limitów po zmianie
		EffectiveApiRateLimits currentLimits = await AsyncPollingUtils.PollAsync(
			action: () => LimitsClient.GetRateLimitsAsync(accessToken, CancellationToken),
			condition: response => AreRateLimitsEqual(modifiedLimits, response),
			delay: RateLimitsStatePollingDelay,
			maxAttempts: MaxAttempts,
			cancellationToken: CancellationToken);

		// Assert: Weryfikacja, że limity zostały zmienione zgodnie z oczekiwaniami
		AssertRateLimitsEqual(modifiedLimits, currentLimits);

		// Act: Przywrócenie wartości domyślnych
		await TestDataClient.RestoreRateLimitsAsync(accessToken);

		// Act: Ponowne pobranie po przywróceniu
		EffectiveApiRateLimits restoredLimits = await AsyncPollingUtils.PollAsync(
			action: () => LimitsClient.GetRateLimitsAsync(accessToken, CancellationToken),
			condition: response => AreRateLimitsEqual(originalLimits, response),
			delay: RateLimitsStatePollingDelay,
			maxAttempts: MaxAttempts,
			cancellationToken: CancellationToken);

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
                MiscellaneousUtils.GetRandomNip());
        string accessToken = authorizationInfo.AccessToken.Token;

        // Arrange: Pobranie aktualnych limitów do bazowania
        EffectiveApiRateLimits baseLimits =
            await LimitsClient.GetRateLimitsAsync(
                accessToken,
                CancellationToken);
        Assert.NotNull(baseLimits);

        // Arrange: Przygotowanie jawnie nieprawidłowych wartości (OnlineSession poniżej minimum)
        EffectiveApiRateLimits invalidLimits = new()
        {
            OnlineSession = new EffectiveApiRateLimitValues
            {
                PerSecond = 0,
                PerMinute = 0,
                PerHour = 0
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
            InvoiceExportStatus = baseLimits.InvoiceExportStatus,
            InvoiceDownload = baseLimits.InvoiceDownload,
            Other = baseLimits.Other
        };

        EffectiveApiRateLimitsRequest request = new()
        {
            RateLimits = invalidLimits
        };

        // Act
        KsefApiException exception = await Assert.ThrowsAsync<KsefApiException>(
            () => TestDataClient.SetRateLimitsAsync(request, accessToken));

        // Assert
        Assert.Contains("21405", exception.Message);
    }

    /// <summary>
    /// Weryfikuje limit endpointu z grupy "Pozostałe" przy równoległych żądaniach.
    /// Po wspólnym starcie część żądań powinna przejść, a część zakończyć się HTTP 429.
    /// </summary>
    [Fact]
    public async Task RateLimits_E2E_CurrentContextEndpoint_ShouldRateLimitConcurrentRequests()
    {
        if (ShouldSkipTestDataRateLimitsTest())
        {
            return;
        }

        string contextNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip).ConfigureAwait(false);

        string ownerAccessToken = ownerAuth.AccessToken.Token;

        try
        {
            EffectiveApiRateLimits originalLimits =
                await LimitsClient.GetRateLimitsAsync(ownerAccessToken, CancellationToken).ConfigureAwait(false);

            EffectiveApiRateLimitsRequest rateLimitsRequest =
                CreateRateLimitsWithLowOtherPerSecond(originalLimits);

            await SetAndVerifyOtherPerSecondLimitAsync(
                rateLimitsRequest,
                ownerAccessToken,
                contextNip).ConfigureAwait(false);

            await Task.Delay(RateLimiterPropagationDelay).ConfigureAwait(false);

            IReadOnlyList<Exception?> results =
                await ExecuteConcurrentCurrentContextEndpointRequestsAsync(ownerAccessToken, MaxRequestsToReachOtherPerSecondLimit).ConfigureAwait(false);

            AssertRateLimitReached(
                results,
                $"Burst traffic dla kontekstu {contextNip} powinien zwrócić co najmniej jedno HTTP 429.");

            int successCount = results.Count(exception => exception is null);
            int rateLimitedCount = results.Count(exception => exception is KsefRateLimitException);

            Assert.True(successCount > 0,
                $"Burst traffic dla kontekstu {contextNip} powinien przepuścić co najmniej jedno żądanie przed zadziałaniem limitu.");
            Assert.True(rateLimitedCount > 0,
                $"Burst traffic dla kontekstu {contextNip} powinien ograniczyć co najmniej jedno żądanie HTTP 429.");
            Assert.Equal(results.Count, successCount + rateLimitedCount);
        }
        finally
        {
            await RestoreRateLimitsWithRetryAsync(ownerAccessToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Weryfikuje, że limit 429 jest liczony dla bieżącego kontekstu i IP,
    /// a nie zbiorczo dla tego samego identyfikatora użytego w różnych kontekstach.
    /// Mechanika testu wynika z dokumentacji limitów: liczniki są niezależne dla pary
    /// ContextIdentifier + IP, a dla endpointów z grupy "Pozostałe" każdy endpoint ma własny licznik.
    /// </summary>
    [Fact]
    public async Task RateLimits_E2E_SameIdentifierDifferentContexts_ShouldBeCountedPerContext()
    {
        if (ShouldSkipTestDataRateLimitsTest())
        {
            return;
        }

        string contextOverLimitNip = MiscellaneousUtils.GetRandomNip();
        string contextStillBelowLimitNip = MiscellaneousUtils.GetRandomNip();
        string authorizedNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse contextOverLimitOwnerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextOverLimitNip);
        AuthenticationOperationStatusResponse contextStillBelowLimitOwnerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextStillBelowLimitNip);

        string contextOverLimitOwnerToken = contextOverLimitOwnerAuth.AccessToken.Token;
        string contextStillBelowLimitOwnerToken = contextStillBelowLimitOwnerAuth.AccessToken.Token;

        await GrantInvoiceReadPermissionAsync(contextOverLimitNip, authorizedNip);
        await GrantInvoiceReadPermissionAsync(contextStillBelowLimitNip, authorizedNip);

        AuthenticationOperationStatusResponse contextOverLimitAuth =
            await AuthenticateAuthorizedInContextAsync(authorizedNip, contextOverLimitNip);
        AuthenticationOperationStatusResponse contextStillBelowLimitAuth =
            await AuthenticateAuthorizedInContextAsync(authorizedNip, contextStillBelowLimitNip);

        string contextOverLimitToken = contextOverLimitAuth.AccessToken.Token;
        string contextStillBelowLimitToken = contextStillBelowLimitAuth.AccessToken.Token;

        try
        {
            EffectiveApiRateLimits contextOverLimitLimits =
                await LimitsClient.GetRateLimitsAsync(contextOverLimitOwnerToken, CancellationToken);
            EffectiveApiRateLimits contextStillBelowLimitLimits =
                await LimitsClient.GetRateLimitsAsync(contextStillBelowLimitOwnerToken, CancellationToken);

            EffectiveApiRateLimitsRequest contextOverLimitRateLimitsRequest =
                CreateRateLimitsWithLowOtherPerSecond(contextOverLimitLimits);
            EffectiveApiRateLimitsRequest contextStillBelowLimitRateLimitsRequest =
                CreateRateLimitsWithLowOtherPerSecond(contextStillBelowLimitLimits);

            await SetAndVerifyOtherPerSecondLimitAsync(
                contextOverLimitRateLimitsRequest,
                contextOverLimitOwnerToken,
                contextOverLimitNip);

            await SetAndVerifyOtherPerSecondLimitAsync(
                contextStillBelowLimitRateLimitsRequest,
                contextStillBelowLimitOwnerToken,
                contextStillBelowLimitNip);

            await Task.Delay(RateLimiterPropagationDelay).ConfigureAwait(false);

            IReadOnlyList<Exception?> contextOverLimitResults =
                await ExecuteSequentialCurrentContextEndpointRequestsAsync(contextOverLimitToken, MaxRequestsToReachOtherPerSecondLimit);
            AssertRateLimitReached(
                contextOverLimitResults,
                $"Kontekst {contextOverLimitNip} powinien dostać 429 po przekroczeniu własnego limitu dla IP.");

            await AssertNoRateLimitAsync(
                () => LimitsClient.GetLimitsForCurrentContextAsync(contextStillBelowLimitToken, CancellationToken),
                $"Kontekst {contextStillBelowLimitNip} nie powinien dostać 429 po przekroczeniu limitu w kontekście {contextOverLimitNip} dla tego samego identyfikatora {authorizedNip}.");

            IReadOnlyList<Exception?> contextStillBelowLimitResults =
                await ExecuteSequentialCurrentContextEndpointRequestsAsync(contextStillBelowLimitToken, MaxRequestsToReachOtherPerSecondLimit);
            AssertRateLimitReached(
                contextStillBelowLimitResults,
                $"Kontekst {contextStillBelowLimitNip} powinien dostać 429 dopiero po przekroczeniu własnego limitu dla IP.");
        }
        finally
        {
            await RestoreRateLimitsWithRetryAsync(contextOverLimitOwnerToken);
            await RestoreRateLimitsWithRetryAsync(contextStillBelowLimitOwnerToken);

            await RevokePermissionsWithRetryAsync(contextOverLimitNip, authorizedNip);
            await RevokePermissionsWithRetryAsync(contextStillBelowLimitNip, authorizedNip);
        }
    }

    private static bool ShouldSkipTestDataRateLimitsTest()
    {
        string baseUrl = TestConfig.GetApiSettings().BaseUrl?.TrimEnd('/') ?? string.Empty;

        return IsSameBaseUrl(baseUrl, KsefEnvironmentsUris.PROD)
            || IsSameBaseUrl(baseUrl, KsefEnvironmentsUris.DEMO);
    }

    private static bool IsSameBaseUrl(string actual, string expected)
        => string.Equals(actual.TrimEnd('/'), expected.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

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
            InvoiceExportStatus = source.InvoiceExportStatus,
            InvoiceDownload = ModifyWithinBounds(source.InvoiceDownload, delta, InvoiceDownloadMax),
            Other = ModifyWithinBounds(source.Other, delta, OtherMax)
        };
    }

    private static EffectiveApiRateLimitsRequest CreateRateLimitsWithLowOtherPerSecond(EffectiveApiRateLimits source)
    {
        return new EffectiveApiRateLimitsRequest
        {
            RateLimits = new EffectiveApiRateLimits
            {
                OnlineSession = source.OnlineSession,
                BatchSession = source.BatchSession,
                InvoiceSend = source.InvoiceSend,
                InvoiceStatus = source.InvoiceStatus,
                SessionList = source.SessionList,
                SessionInvoiceList = source.SessionInvoiceList,
                SessionMisc = source.SessionMisc,
                InvoiceMetadata = source.InvoiceMetadata,
                InvoiceExport = source.InvoiceExport,
                InvoiceExportStatus = source.InvoiceExportStatus,
                InvoiceDownload = source.InvoiceDownload,
                Other = new EffectiveApiRateLimitValues
                {
                    PerSecond = LowOtherPerSecondLimit,
                    PerMinute = source.Other.PerMinute,
                    PerHour = source.Other.PerHour
                }
            }
        };
    }

    private async Task GrantInvoiceReadPermissionAsync(string contextNip, string authorizedNip)
    {
        TestDataPermissionsGrantRequest grantRequest = new()
        {
            AuthorizedIdentifier = new AuthorizedIdentifier
            {
                Type = AuthorizedIdentifierType.Nip,
                Value = authorizedNip
            },
            ContextIdentifier = new KSeF.Client.Core.Models.TestData.ContextIdentifier
            {
                Value = contextNip
            },
            Permissions =
            [
                new Permission
                {
                    PermissionType = PermissionType.InvoiceRead,
                    Description = "Rate limits per context E2E"
                }
            ]
        };

        await TestDataClient.GrantPermissionsAsync(grantRequest, CancellationToken).ConfigureAwait(false);
    }

    private async Task RevokePermissionsAsync(string contextNip, string authorizedNip)
    {
        TestDataPermissionsRevokeRequest revokeRequest = new()
        {
            AuthorizedIdentifier = new AuthorizedIdentifier
            {
                Type = AuthorizedIdentifierType.Nip,
                Value = authorizedNip
            },
            ContextIdentifier = new KSeF.Client.Core.Models.TestData.ContextIdentifier
            {
                Value = contextNip
            }
        };

        await TestDataClient.RevokePermissionsAsync(revokeRequest, CancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthenticationOperationStatusResponse> AuthenticateAuthorizedInContextAsync(
        string authorizedNip,
        string contextNip)
    {
        AuthenticationOperationStatusResponse? response = await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                try
                {
                    return await AuthenticationUtils.AuthenticateAsync(
                        AuthorizationClient,
                        authorizedNip,
                        contextNip).ConfigureAwait(false);
                }
                catch
                {
                    return null;
                }
            },
            condition: authResponse => authResponse is not null,
            delay: PermissionPropagationPollingDelay,
            maxAttempts: PermissionPropagationMaxAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        return response!;
    }

    private async Task SetAndVerifyOtherPerSecondLimitAsync(
        EffectiveApiRateLimitsRequest request,
        string ownerAccessToken,
        string contextNip)
    {
        EffectiveApiRateLimits? lastLimits = null;

        for (int attempt = 1; attempt <= RateLimitChangeMaxAttempts; attempt++)
        {
            await TestDataClient.SetRateLimitsAsync(request, ownerAccessToken, CancellationToken).ConfigureAwait(false);

            try
            {
                lastLimits = await AsyncPollingUtils.PollAsync(
                    action: () => LimitsClient.GetRateLimitsAsync(ownerAccessToken, CancellationToken),
                    condition: limits => limits.Other.PerSecond == LowOtherPerSecondLimit,
                    description: $"Limit Other.PerSecond dla kontekstu {contextNip} powinien zostać zastosowany.",
                    delay: RateLimitChangeVerificationDelay,
                    maxAttempts: RateLimitChangeVerificationMaxAttempts,
                    cancellationToken: CancellationToken).ConfigureAwait(false);

                return;
            }
            catch (TimeoutException) when (attempt < RateLimitChangeMaxAttempts)
            {
                lastLimits = await LimitsClient.GetRateLimitsAsync(ownerAccessToken, CancellationToken).ConfigureAwait(false);
            }
        }

        Assert.Fail(
            $"Kontekst {contextNip} powinien mieć ustawiony limit Other.PerSecond={LowOtherPerSecondLimit}, a widzi {lastLimits?.Other.PerSecond}.");
    }

    private async Task<IReadOnlyList<Exception?>> ExecuteSequentialCurrentContextEndpointRequestsAsync(string accessToken, int maxCount)
    {
        // GET /limits/context jest endpointem z grupy "Pozostałe". Używamy go jako osobnego
        // licznika, żeby wcześniejsze GET /rate-limits z setupu nie wpływały na właściwą asercję.
        List<Exception?> results = new(capacity: maxCount);

        for (int i = 0; i < maxCount; i++)
        {
            Exception? exception = await CaptureExceptionAsync(
                () => LimitsClient.GetLimitsForCurrentContextAsync(accessToken, CancellationToken)).ConfigureAwait(false);

            results.Add(exception);

            if (exception is not null && exception is not KsefRateLimitException)
            {
                throw exception;
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<Exception?>> ExecuteConcurrentCurrentContextEndpointRequestsAsync(string accessToken, int requestCount)
    {
        TaskCompletionSource<bool> startSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<Exception?>[] tasks = Enumerable.Range(0, requestCount)
            .Select(_ => ExecuteCurrentContextRequestWithSharedStartAsync(accessToken, startSignal.Task))
            .ToArray();

        startSignal.SetResult(true);

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<Exception?> ExecuteCurrentContextRequestWithSharedStartAsync(string accessToken, Task startSignal)
    {
        await startSignal.ConfigureAwait(false);

        return await CaptureExceptionAsync(
            () => LimitsClient.GetLimitsForCurrentContextAsync(accessToken, CancellationToken)).ConfigureAwait(false);
    }

    private static void AssertRateLimitReached(IReadOnlyList<Exception?> results, string message)
    {
        Assert.True(results.Any(exception => exception is KsefRateLimitException), message);
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task AssertNoRateLimitAsync(Func<Task> action, string message)
    {
        Exception? exception = await CaptureExceptionAsync(action).ConfigureAwait(false);

        Assert.False(exception is KsefRateLimitException, message);

        if (exception is not null)
        {
            throw exception;
        }
    }

    private async Task RestoreRateLimitsWithRetryAsync(string accessToken)
    {
        await ExecuteWithRateLimitRetryAsync(
            () => TestDataClient.RestoreRateLimitsAsync(accessToken, CancellationToken)).ConfigureAwait(false);
    }

    private async Task RevokePermissionsWithRetryAsync(string contextNip, string authorizedNip)
    {
        await ExecuteWithRateLimitRetryAsync(
            () => RevokePermissionsAsync(contextNip, authorizedNip)).ConfigureAwait(false);
    }

    private static async Task ExecuteWithRateLimitRetryAsync(Func<Task> action)
    {
        await AsyncPollingUtils.PollAsync(
            check: async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            },
            description: "Cleanup po teście limitów powinien zakończyć się mimo chwilowego HTTP 429.",
            delay: RateLimitRetryDelay,
            maxAttempts: CleanupRateLimitMaxAttempts,
            shouldRetryOnException: exception => exception is KsefRateLimitException,
            rateLimitOnException: exception =>
            {
                KsefRateLimitException rateLimitException = (KsefRateLimitException)exception;
                TimeSpan delay = rateLimitException.RecommendedDelay > RateLimitRetryDelay
                    ? rateLimitException.RecommendedDelay
                    : RateLimitRetryDelay;

                return new AsyncPollingUtils.RateLimitDecision(IsRateLimited: true, DelayOverride: delay);
            },
            cancellationToken: CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Modyfikuje wartości limitów jednej kategorii, pozostając w dopuszczalnych granicach.
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
        // Jeśli bieżąca wartość wykracza poza nasze oczekiwane widełki, nie wiemy jakie są
        // rzeczywiste limity API dla tej wartości – pozostawiamy bez zmian.
        if (current < min || current > max)
        {
            return current;
        }

        if (current + delta <= max)
        {
            return current + delta;
        }

        if (current - delta >= min)
        {
            return current - delta;
        }

        return current;
    }

    private static bool AreRateLimitsEqual(EffectiveApiRateLimits expected, EffectiveApiRateLimits actual)
    {
        return AreRateLimitValuesEqual(expected.OnlineSession, actual.OnlineSession)
            && AreRateLimitValuesEqual(expected.BatchSession, actual.BatchSession)
            && AreRateLimitValuesEqual(expected.InvoiceSend, actual.InvoiceSend)
            && AreRateLimitValuesEqual(expected.InvoiceStatus, actual.InvoiceStatus)
            && AreRateLimitValuesEqual(expected.SessionList, actual.SessionList)
            && AreRateLimitValuesEqual(expected.SessionInvoiceList, actual.SessionInvoiceList)
            && AreRateLimitValuesEqual(expected.SessionMisc, actual.SessionMisc)
            && AreRateLimitValuesEqual(expected.InvoiceMetadata, actual.InvoiceMetadata)
            && AreRateLimitValuesEqual(expected.InvoiceExport, actual.InvoiceExport)
            && AreRateLimitValuesEqual(expected.InvoiceExportStatus, actual.InvoiceExportStatus)
            && AreRateLimitValuesEqual(expected.InvoiceDownload, actual.InvoiceDownload)
            && AreRateLimitValuesEqual(expected.Other, actual.Other);
    }

    private static bool AreRateLimitValuesEqual(EffectiveApiRateLimitValues? expected, EffectiveApiRateLimitValues? actual)
    {
        if (expected is null || actual is null)
        {
            return expected is null && actual is null;
        }

        return expected.PerSecond == actual.PerSecond
            && expected.PerMinute == actual.PerMinute
            && expected.PerHour == actual.PerHour;
    }

    private static void AssertRateLimitValuesEqual(EffectiveApiRateLimitValues? expected, EffectiveApiRateLimitValues? actual)
    {
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null);
            return;
        }

        Assert.Equal(expected.PerSecond, actual.PerSecond);
        Assert.Equal(expected.PerMinute, actual.PerMinute);
        Assert.Equal(expected.PerHour, actual.PerHour);
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
        // InvoiceExportStatus
        AssertRateLimitValuesEqual(expected.InvoiceExportStatus, actual.InvoiceExportStatus);
        // InvoiceDownload
        Assert.Equal(expected.InvoiceDownload.PerSecond, actual.InvoiceDownload.PerSecond);
        Assert.Equal(expected.InvoiceDownload.PerMinute, actual.InvoiceDownload.PerMinute);
        Assert.Equal(expected.InvoiceDownload.PerHour, actual.InvoiceDownload.PerHour);
        // Other
        Assert.Equal(expected.Other.PerSecond, actual.Other.PerSecond);
        Assert.Equal(expected.Other.PerMinute, actual.Other.PerMinute);
        Assert.Equal(expected.Other.PerHour, actual.Other.PerHour);
    }
}
