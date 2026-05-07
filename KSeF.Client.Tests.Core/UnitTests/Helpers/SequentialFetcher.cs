#nullable enable
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;

namespace KSeF.Client.Tests.Core.UnitTests.Helpers;

/// <summary>
/// Mock <see cref="ICertificateFetcher"/> zwracający kolejne odpowiedzi z kolejki.
/// Przydatny do symulacji rotacji kluczy i re-certyfikacji.
/// </summary>
internal sealed class SequentialFetcher : ICertificateFetcher
{
    private readonly Queue<ICollection<PemCertificateInfo>> _responses;

    public int CallCount { get; private set; }

    public SequentialFetcher(params ICollection<PemCertificateInfo>[] responses)
    {
        _responses = new Queue<ICollection<PemCertificateInfo>>(responses);
    }

    public Task<ICollection<PemCertificateInfo>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        if (_responses.Count == 0)
            throw new InvalidOperationException("SequentialFetcher: brak kolejnych odpowiedzi w kolejce.");
        return Task.FromResult(_responses.Dequeue());
    }
}
