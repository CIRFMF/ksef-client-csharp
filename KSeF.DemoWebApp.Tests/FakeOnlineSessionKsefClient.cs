using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;

namespace KSeF.DemoWebApp.Tests;

/// <summary>
/// Atrapa <see cref="IKSeFClient"/> obsługująca wyłącznie sesję interaktywną.
/// Zapamiętuje odkodowaną treść każdej wysyłki, aby test mógł sprawdzić, jakim
/// kluczem zaszyfrowano fakturę. Pozostałe operacje nie są wspierane.
/// </summary>
internal sealed partial class FakeOnlineSessionKsefClient : IKSeFClient
{
    private int openedSessions;

    public List<(string SessionReferenceNumber, string EncryptedInvoiceContent)> SentInvoices { get; } = [];

    public Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(OpenOnlineSessionRequest requestPayload, string accessToken, string upoVersion = null, CancellationToken cancellationToken = default)
    {
        openedSessions++;

        return Task.FromResult(new OpenOnlineSessionResponse
        {
            ReferenceNumber = $"session-{openedSessions}",
        });
    }

    public Task<SendInvoiceResponse> SendOnlineSessionInvoiceAsync(SendInvoiceRequest requestPayload, string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
    {
        string encryptedInvoiceContent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(requestPayload.EncryptedInvoiceContent));

        SentInvoices.Add((sessionReferenceNumber, encryptedInvoiceContent));

        return Task.FromResult(new SendInvoiceResponse
        {
            ReferenceNumber = $"invoice-{SentInvoices.Count}",
        });
    }

    public Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accessToken, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
