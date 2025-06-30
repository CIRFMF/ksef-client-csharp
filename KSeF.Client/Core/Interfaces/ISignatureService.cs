using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Core.Interfaces;

public interface ISignatureService
{
    public Task<string> Sign(string xml, X509Certificate2 certificate);
}
