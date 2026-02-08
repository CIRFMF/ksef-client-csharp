using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Models.Sessions.BatchSession;

namespace KSeF.Client.Helpers;

/// <summary>
/// Klasa pomocnicza do wysyłki części paczki wsadowej.
/// </summary>
public static class BatchPartsSender
{
    public static async Task SendPackagePartsAsync<TInfo>(
        IRestClient restClient,
        ICollection<PackagePartSignatureInitResponseType> parts,
        ICollection<TInfo> batchPartSendingInfos,
        Func<TInfo, HttpContent> contentFactory,
        CancellationToken cancellationToken = default)
        where TInfo : class
    {
        Guard.ThrowIfNull(restClient);

        if (parts == null)
        {
            throw new InvalidOperationException("Brak informacji o częściach paczki do wysłania.");
        }

        List<string> errors = new();

        foreach (PackagePartSignatureInitResponseType part in parts)
        {
            TInfo fileInfo = batchPartSendingInfos?.FirstOrDefault(x =>
                (int)x.GetType().GetProperty("OrdinalNumber")!.GetValue(x)! == part.OrdinalNumber);

            if (fileInfo == null)
            {
                errors.Add($"Brak danych dla części paczki {part.OrdinalNumber}.");
                continue;
            }

            using HttpContent content = contentFactory(fileInfo);

            if (string.IsNullOrWhiteSpace(part.Method))
            {
                errors.Add($"Brak metody HTTP dla części paczki {part.OrdinalNumber}.");
                continue;
            }

            try
            {
                await restClient.SendAsync(
                    new HttpMethod(part.Method.ToUpperInvariant()),
                    part.Url.ToString(),
                    content,
                    part.Headers,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errors.Add($"Błąd wysyłki części paczki {part.OrdinalNumber}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "Wystąpiły błędy podczas wysyłania części paczki.",
                errors.Select(e => new InvalidOperationException(e))
            );
        }
    }
}
