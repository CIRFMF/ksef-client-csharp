using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;
using System.Security.Cryptography;

namespace KSeF.Client.Core.Interfaces;

/// <summary>
/// Zarządza operacjami kryptograficznymi, takimi jak szyfrowanie danych, generowanie żądań podpisu certyfikatu (CSR) oraz metadanych plików.
/// </summary>
public interface ICryptographyService
{
    /// <summary>
    /// Zwraca dane szyfrowania, w tym klucz szyfrowania, wektor IV i zaszyfrowany klucz.
    /// </summary>
    /// <returns><see cref="EncryptionData"/></returns>
    EncryptionData GetEncryptionData();

    /// <summary>
    /// Szyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7 paddingiem.
    /// </summary>
    /// <param name="content">Plik w formie byte array.</param>
    /// <param name="key">Klucz symetryczny.</param>
    /// <param name="iv">Wektro IV klucza symetrycznego.</param>
    /// <returns>Zaszyfrowany plik w formie byte array.</returns>
    byte[] EncryptBytesWithAES256(byte[] content, byte[] key, byte[] iv);
    /// <summary>
    /// Generuje żądanie podpisania certyfikatu (CSR) na podstawie przekazanych informacji o certyfikacie.
    /// </summary>
    /// <param name="certificateInfo"></param>
    /// <returns>Zwraca CSR oraz klucz prywatny, oba zakodowane w Base64</returns>
    (string, string) GenerateCsr(CertificateEnrollmentsInfoResponse certificateInfo);

    /// <summary>
    /// Zwraca metadane plik: rozmiar i hash SHA256.
    /// </summary>
    /// <param name="file">Plik w formie byte array</param>
    /// <returns><see cref="FileMetadata"/></returns>
    FileMetadata GetMetaData(byte[] file);
    /// <summary>
    /// Zwraca zaszyfrowany plik formie byte array przy użyciu algorytmu RSA.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="padding"></param>
    /// <returns></returns>
    byte[] EncryptWithRSAUsingPublicKey(byte[] content, RSAEncryptionPadding padding);

    /// <summary>
    /// Zwraca zaszyfrowany token KSeF przy użyciu algorytmu RSA z publicznym kluczem.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    byte[] EncryptKsefTokenWithRSAUsingPublicKey(byte[] content);
}
