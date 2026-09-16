using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.DemoWebApp.Tests;

/// <summary>
/// Atrapa <see cref="ICryptographyService"/> bez zależności sieciowych.
/// Każde wywołanie <see cref="GetEncryptionData"/> zwraca nowy, rozpoznawalny klucz,
/// a szyfrowanie jest podmianą treści na jawny znacznik użytego klucza — dzięki temu
/// test może stwierdzić, którym kluczem zaszyfrowano daną fakturę.
/// </summary>
internal sealed class FakeCryptographyService : ICryptographyService
{
    private int issuedKeys;

    public EncryptionData GetEncryptionData()
    {
        issuedKeys++;

        byte[] cipherKey = new byte[32];
        cipherKey[0] = (byte)issuedKeys;

        return new EncryptionData
        {
            CipherKey = cipherKey,
            CipherIv = new byte[16],
            EncryptionInfo = new EncryptionInfo
            {
                EncryptedSymmetricKey = $"key-{issuedKeys}",
                InitializationVector = "iv",
            },
        };
    }

    public byte[] EncryptBytesWithAES256(byte[] content, byte[] key, byte[] iv)
        => System.Text.Encoding.UTF8.GetBytes($"encrypted-with-key-{key[0]}");

    public FileMetadata GetMetaData(byte[] file)
        => new()
        {
            FileSize = file.Length,
            HashSHA = Convert.ToBase64String(SHA256.HashData(file)),
        };

    public FileMetadata GetMetaData(Stream fileStream)
        => throw new NotSupportedException();

    public Task<FileMetadata> GetMetaDataAsync(Stream fileStream, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public bool IsWarmedUp()
        => throw new NotSupportedException();

    public void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv)
        => throw new NotSupportedException();

    public Task EncryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public byte[] DecryptBytesWithAES256(byte[] content, byte[] key, byte[] iv)
        => throw new NotSupportedException();

    public void DecryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv)
        => throw new NotSupportedException();

    public Task DecryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public byte[] EncryptWithRSAUsingPublicKey(byte[] content, RSAEncryptionPadding padding)
        => throw new NotSupportedException();

    public byte[] EncryptKsefTokenWithRSAUsingPublicKey(byte[] content)
        => throw new NotSupportedException();

    public byte[] EncryptWithECDSAUsingPublicKey(byte[] content)
        => throw new NotSupportedException();

    public Task WarmupAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ForceRefreshAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public (string, string) GenerateCsrWithRsa(CertificateEnrollmentsInfoResponse certificateInfo, RSASignaturePadding padding = null)
        => throw new NotSupportedException();

    public (string, string) GenerateCsrWithEcdsa(CertificateEnrollmentsInfoResponse certificateInfo)
        => throw new NotSupportedException();

    public void SetExternalMaterials(X509Certificate2 symmetricKeyCert, X509Certificate2 ksefTokenCert, string? symmetricKeyPublicKeyId = null, string? ksefTokenPublicKeyId = null)
        => throw new NotSupportedException();

    public X509Certificate2 SymmetricKeyCertificate => throw new NotSupportedException();

    public X509Certificate2 KsefTokenCertificate => throw new NotSupportedException();

    public string? SymmetricKeyPublicKeyId => throw new NotSupportedException();

    public string? KsefTokenPublicKeyId => throw new NotSupportedException();
}
