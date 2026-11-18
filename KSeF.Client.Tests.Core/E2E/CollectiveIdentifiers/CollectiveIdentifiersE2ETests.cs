using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.CollectiveIdentifiers;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.CollectiveIdentifiers;

public class CollectiveIdentifiersE2ETests : TestBase
{
    private const string InvoiceTemplate = "invoice-template-fa-3-with-custom-Subject2.xml";
    private const int MaxPollingAttempts = 30;
    private const int InvoicesCount = 5;
    private const int FirstGroupInvoicesCount = 3;
    private const int SecondGroupInvoicesCount = 2;

    /// <summary>
    /// Weryfikuje pełny cykl generowania dwóch odrębnych identyfikatorów zbiorczych dla rozłącznych grup faktur sprzedawcy
    /// Kroki:
    /// 1) Sprzedawca wystawia kilka faktur i czeka na ich dostępność w systemie
    /// 2) Wystawione faktury dzielone są na dwie rozłączne grupy, dla każdej generowany jest osobny identyfikator zbiorczy
    /// 3) Weryfikacja odnalezienia obu identyfikatorów po numerze KSeF faktury z odpowiedniej grupy
    /// 4) Jednym żądaniem (`GetCollectiveIdentifierInvoicesAsync` z listą obu numerów) weryfikacja odnalezienia
    ///    wszystkich faktur z obu grup, każda poprawnie przypisana do właściwego identyfikatora zbiorczego
    /// 5) Weryfikacja odnalezienia obu identyfikatorów w wynikach zapytania listującego
    /// </summary>
    [Fact]
    public async Task GenerateCollectiveIdentifiers_ThenFindThemByKsefNumberAndByNumber()
    {
        string sellerNip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse sellerAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, sellerNip);
        string sellerToken = sellerAuth.AccessToken.Token;

        string buyerNip = MiscellaneousUtils.GetRandomNip();

        // Dwie rozłączne grupy faktur - każda trafia do osobnego identyfikatora zbiorczego.
        List<string> firstGroupKsefNumbers = await SendInvoicesAndGetKsefNumbersAsync(FirstGroupInvoicesCount, sellerNip, buyerNip, sellerToken);
        List<string> secondGroupKsefNumbers = await SendInvoicesAndGetKsefNumbersAsync(SecondGroupInvoicesCount, sellerNip, buyerNip, sellerToken);
        List<string> ksefNumbers = [.. firstGroupKsefNumbers, .. secondGroupKsefNumbers];

        DateTimeOffset dateFrom = DateTimeOffset.UtcNow.AddMinutes(-5);

        string firstCollectiveIdentifierNumber = await GenerateCollectiveIdentifierAsync(firstGroupKsefNumbers, sellerToken);
        string secondCollectiveIdentifierNumber = await GenerateCollectiveIdentifierAsync(secondGroupKsefNumbers, sellerToken);

        foreach ((string ksefNumber, string collectiveIdentifierNumber) in new[]
        {
            (firstGroupKsefNumbers[0], firstCollectiveIdentifierNumber),
            (secondGroupKsefNumbers[0], secondCollectiveIdentifierNumber)
        })
        {
            CollectiveIdentifiersByKsefNumberQueryResponse byKsefNumber = await AsyncPollingUtils.PollAsync(
                action: () => CollectiveIdentifiersClient.GetCollectiveIdentifiersByKsefNumberAsync(
                    ksefNumber, sellerToken, pageSize: 10, cancellationToken: CancellationToken),
                condition: r => r?.CollectiveIdentifiers is not null && r.CollectiveIdentifiers.Any(ci => ci.CollectiveIdentifierNumber == collectiveIdentifierNumber),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: MaxPollingAttempts,
                cancellationToken: CancellationToken);

            Assert.NotNull(byKsefNumber);
            Assert.Contains(byKsefNumber.CollectiveIdentifiers, ci => ci.CollectiveIdentifierNumber == collectiveIdentifierNumber);
        }

        // Jedno żądanie obejmujące oba identyfikatory zbiorcze naraz.
        CollectiveIdentifierInvoicesQueryResponse invoicesResponse = await AsyncPollingUtils.PollAsync(
            action: () => CollectiveIdentifiersClient.GetCollectiveIdentifierInvoicesAsync(
                new CollectiveIdentifierInvoicesQueryRequest
                {
                    CollectiveIdentifierNumbers = [firstCollectiveIdentifierNumber, secondCollectiveIdentifierNumber]
                },
                sellerToken, pageSize: 10, cancellationToken: CancellationToken),
            condition: r => r?.Invoices is not null && ksefNumbers.All(ksefNumber => r.Invoices.Any(inv => inv.KsefNumber == ksefNumber)),
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(invoicesResponse);
        foreach (string ksefNumber in firstGroupKsefNumbers)
        {
            Assert.Contains(invoicesResponse.Invoices, inv => inv.KsefNumber == ksefNumber && inv.CollectiveIdentifierNumber == firstCollectiveIdentifierNumber);
        }
        foreach (string ksefNumber in secondGroupKsefNumbers)
        {
            Assert.Contains(invoicesResponse.Invoices, inv => inv.KsefNumber == ksefNumber && inv.CollectiveIdentifierNumber == secondCollectiveIdentifierNumber);
        }

        CollectiveIdentifiersQueryResponse queryResponse = await AsyncPollingUtils.PollAsync(
            action: () => CollectiveIdentifiersClient.QueryCollectiveIdentifiersAsync(
                new CollectiveIdentifiersQueryRequest
                {
                    DateCreatedFrom = dateFrom,
                    DateCreatedTo = DateTimeOffset.UtcNow.AddMinutes(5)
                },
                sellerToken,
                pageSize: 10,
                cancellationToken: CancellationToken),
            condition: r => r?.CollectiveIdentifiers is not null
                && r.CollectiveIdentifiers.Any(ci => ci.CollectiveIdentifierNumber == firstCollectiveIdentifierNumber)
                && r.CollectiveIdentifiers.Any(ci => ci.CollectiveIdentifierNumber == secondCollectiveIdentifierNumber),
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken);

        Assert.NotNull(queryResponse);
        Assert.Contains(queryResponse.CollectiveIdentifiers, ci => ci.CollectiveIdentifierNumber == firstCollectiveIdentifierNumber);
        Assert.Contains(queryResponse.CollectiveIdentifiers, ci => ci.CollectiveIdentifierNumber == secondCollectiveIdentifierNumber);
    }


	/// <summary>
	/// Wystawia wskazaną liczbę faktur i zwraca ich numery KSeF.
	/// </summary>
	private async Task<List<string>> SendInvoicesAndGetKsefNumbersAsync(int count, string sellerNip, string buyerNip, string sellerToken)
	{
		List<string> ksefNumbers = [];
		for (int i = 0; i < count; i++)
		{
			ksefNumbers.Add(await SendInvoiceAndGetKsefNumberAsync(sellerNip, buyerNip, sellerToken).ConfigureAwait(false));
		}

		return ksefNumbers;
	}

	/// <summary>
	/// Generuje identyfikator zbiorczy dla podanych faktur i zwraca jego numer.
	/// </summary>
	private async Task<string> GenerateCollectiveIdentifierAsync(IEnumerable<string> ksefNumbers, string sellerToken)
	{
		GenerateCollectiveIdentifierResponse generateResponse = await CollectiveIdentifiersClient.GenerateCollectiveIdentifierAsync(
			new GenerateCollectiveIdentifierRequest
			{
				Invoices = ksefNumbers.Select(ksefNumber => new CollectiveIdentifierInvoice { KsefNumber = ksefNumber }).ToList()
			},
			sellerToken, CancellationToken).ConfigureAwait(false);

		Assert.NotNull(generateResponse);
		Assert.False(string.IsNullOrWhiteSpace(generateResponse.CollectiveIdentifierNumber));

		return generateResponse.CollectiveIdentifierNumber;
	}

	/// <summary>
	/// Wystawia fakturę i czeka na jej trwałe zapisanie, zwracając nadany numer KSeF.
	/// </summary>
	private async Task<string> SendInvoiceAndGetKsefNumberAsync(string sellerNip, string buyerNip, string sellerToken)
    {
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        OpenOnlineSessionResponse session = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient, encryptionData, sellerToken).ConfigureAwait(false);

        SendInvoiceResponse invoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient, session.ReferenceNumber, sellerToken, sellerNip, buyerNip,
            InvoiceTemplate, encryptionData, CryptographyService).ConfigureAwait(false);

        await OnlineSessionUtils.CloseOnlineSessionAsync(KsefClient, session.ReferenceNumber, sellerToken).ConfigureAwait(false);

        SessionInvoice processedInvoice = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.GetSessionInvoiceAsync(
                session.ReferenceNumber, invoiceResponse.ReferenceNumber, sellerToken),
            condition: inv => inv?.PermanentStorageDate is not null,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.NotNull(processedInvoice?.KsefNumber);
        string ksefNumber = processedInvoice.KsefNumber;

        await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.GetInvoiceAsync(ksefNumber, sellerToken),
            condition: xml => !string.IsNullOrWhiteSpace(xml),
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: MaxPollingAttempts,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        return ksefNumber;
    }
}
