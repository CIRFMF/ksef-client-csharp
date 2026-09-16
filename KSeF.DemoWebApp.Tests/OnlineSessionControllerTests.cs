using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.DemoWebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KSeF.DemoWebApp.Tests;

/// <summary>
/// Testy jednostkowe <see cref="OnlineSessionController"/> — bez połączenia z KSeF.
/// </summary>
public sealed class OnlineSessionControllerTests
{
    private const string AccessToken = "access-token";
    private const string ContextIdentifier = "1234567890";

    /// <summary>
    /// Dwie sesje otwarte naprzemiennie muszą zachować własne klucze.
    /// Przy współdzielonym polu statycznym faktura z pierwszej sesji zostałaby
    /// zaszyfrowana kluczem drugiej i odrzucona przez KSeF.
    /// </summary>
    [Fact]
    public async Task SendInvoice_WithTwoInterleavedSessions_UsesKeyOfItsOwnSession()
    {
        // Arrange
        FakeOnlineSessionKsefClient ksefClient = new();
        OnlineSessionController controller = CreateController(ksefClient);

        // Act
        string firstSession = await OpenSessionAsync(controller).ConfigureAwait(false);
        string secondSession = await OpenSessionAsync(controller).ConfigureAwait(false);

        await controller.SendInvoiceOnlineSessionAsync(firstSession, AccessToken, CancellationToken.None).ConfigureAwait(false);
        await controller.SendInvoiceOnlineSessionAsync(secondSession, AccessToken, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(
            [(firstSession, "encrypted-with-key-1"), (secondSession, "encrypted-with-key-2")],
            ksefClient.SentInvoices);
    }

    /// <summary>
    /// Wysyłka bez otwartej sesji kończy się czytelnym 400, a nie NullReferenceException.
    /// </summary>
    [Fact]
    public async Task SendInvoice_WithoutOpenSession_ReturnsBadRequest()
    {
        // Arrange
        OnlineSessionController controller = CreateController(new FakeOnlineSessionKsefClient());

        // Act
        ActionResult<SendInvoiceResponse> result = await controller.SendInvoiceOnlineSessionAsync("nieznana-sesja", AccessToken, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Korekta techniczna korzysta z tego samego rejestru sesji co zwykła wysyłka.
    /// </summary>
    [Fact]
    public async Task SendTechnicalCorrection_WithoutOpenSession_ReturnsBadRequest()
    {
        // Arrange
        OnlineSessionController controller = CreateController(new FakeOnlineSessionKsefClient());

        // Act
        ActionResult<SendInvoiceResponse> result = await controller.SendTechnicalCorrectionAsync("nieznana-sesja", "hash", AccessToken, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Zamknięcie sesji zwalnia jej klucz, więc kolejna wysyłka nie ma już czym szyfrować.
    /// </summary>
    [Fact]
    public async Task SendInvoice_AfterSessionClosed_ReturnsBadRequest()
    {
        // Arrange
        FakeOnlineSessionKsefClient ksefClient = new();
        OnlineSessionController controller = CreateController(ksefClient);
        string sessionReferenceNumber = await OpenSessionAsync(controller).ConfigureAwait(false);

        // Act
        await controller.CloseOnlineSessionAsync(sessionReferenceNumber, AccessToken, CancellationToken.None).ConfigureAwait(false);
        ActionResult<SendInvoiceResponse> result = await controller.SendInvoiceOnlineSessionAsync(sessionReferenceNumber, AccessToken, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Wysyłana faktura pochodzi z szablonu FA(3) i ma podstawiony NIP z kontekstu.
    /// </summary>
    [Fact]
    public void InvoiceTemplate_IsFa3AndCopiedToOutput()
    {
        // Arrange
        string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "invoice-template-fa-3.xml");

        // Act
        string template = File.ReadAllText(templatePath);

        // Assert
        Assert.Contains("kodSystemowy=\"FA (3)\"", template, StringComparison.Ordinal);
        Assert.Contains("#nip#", template, StringComparison.Ordinal);
    }

    private static async Task<string> OpenSessionAsync(OnlineSessionController controller)
    {
        ActionResult<OpenOnlineSessionResponse> result = await controller.OpenOnlineSessionAsync(AccessToken, CancellationToken.None).ConfigureAwait(false);
        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        OpenOnlineSessionResponse response = Assert.IsType<OpenOnlineSessionResponse>(okResult.Value);

        return response.ReferenceNumber;
    }

    private static OnlineSessionController CreateController(FakeOnlineSessionKsefClient ksefClient)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tools:contextIdentifier"] = ContextIdentifier,
            })
            .Build();

        return new OnlineSessionController(ksefClient, new FakeCryptographyService(), configuration);
    }
}
