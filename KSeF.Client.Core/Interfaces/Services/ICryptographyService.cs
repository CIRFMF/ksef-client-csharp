using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// Zarządza operacjami kryptograficznymi, takimi jak szyfrowanie danych, generowanie żądań podpisu certyfikatu (CSR) oraz metadanych plików.
    /// </summary>
    public interface ICryptographyService
    {
        /// <summary>
        /// Zwraca dane szyfrowania, w tym klucz szyfrowania, wektor inicjalizujący (IV) i zaszyfrowany klucz.
        /// </summary>
        /// <returns><see cref="EncryptionData"/></returns>
        EncryptionData GetEncryptionData();

        /// <summary>
        /// Szyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="content">Plik w formie tablicy bajtów.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektor inicjalizujący (IV) klucza symetrycznego.</param>
        /// <returns>Zaszyfrowany plik w formie tablicy bajtów.</returns>
        byte[] EncryptBytesWithAES256(byte[] content, byte[] key, byte[] iv);

        /// <summary>
        /// Szyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="input">Input stream - niezaszyfrowany.</param>
        /// <param name="output">Output stream - zaszyfrowany.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektro IV klucza symetrycznego.</param>
        /// <returns>Zaszyfrowany plik w formie stream.</returns>
        void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv);

        /// <summary>
        /// Asynchroniczne szyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="input">Input stream - niezaszyfrowany.</param>
        /// <param name="output">Output stream - zaszyfrowany.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektor inicjalizujący (IV) klucza symetrycznego.</param>
        /// <param name="cancellationToken">Token anulowania.</param>
        Task EncryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deszyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="content">Zaszyfrowany plik w formie tablicy bajtów.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektor inicjalizujący (IV) klucza symetrycznego.</param>
        /// <returns>Odszyfrowany plik w formie tablicy bajtów.</returns>
        byte[] DecryptBytesWithAES256(byte[] content, byte[] key, byte[] iv);

        /// <summary>
        /// Deszyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="input">Input stream - zaszyfrowany.</param>
        /// <param name="output">Output stream - odszyfrowany.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektor inicjalizujący (IV) klucza symetrycznego.</param>
        void DecryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv);

        /// <summary>
        /// Asynchroniczne deszyfrowanie danych przy użyciu AES-256 w trybie CBC z PKCS7.
        /// </summary>
        /// <param name="input">Input stream - zaszyfrowany.</param>
        /// <param name="output">Output stream - odszyfrowany.</param>
        /// <param name="key">Klucz symetryczny.</param>
        /// <param name="iv">Wektor inicjalizujący (IV) klucza symetrycznego.</param>
        /// <param name="cancellationToken">Token anulowania.</param>
        Task DecryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generuje żądanie podpisania certyfikatu (CSR) z użyciem RSA na podstawie przekazanych informacji o certyfikacie.
        /// </summary>
        /// <param name="certificateInfo"><see cref="CertificateEnrollmentsInfoResponse"/></param>
        /// <param name="padding">Padding Pss jeżeli niepodany.</param>
        /// <returns>Zwraca CSR oraz klucz prywatny, oba zakodowane w Base64 w formacie DER</returns>
        (string, string) GenerateCsrWithRsa(CertificateEnrollmentsInfoResponse certificateInfo, RSASignaturePadding padding = null);

        /// <summary>
        /// Generuje żądanie podpisania certyfikatu (CSR) z użyciem krzywej eliptycznej (EC) na podstawie przekazanych informacji o certyfikacie.
        /// </summary>
        /// <param name="certificateInfo"></param>
        /// <returns>Zwraca CSR oraz klucz prywatny, oba zakodowane w Base64</returns>
        (string, string) GenerateCsrWithEcdsa(CertificateEnrollmentsInfoResponse certificateInfo);

        /// <summary>
        /// Zwraca metadane plik: rozmiar i hash SHA256.
        /// </summary>
        /// <param name="file">Plik w formie tablicy bajtów</param>
        /// <returns><see cref="FileMetadata"/></returns>
        FileMetadata GetMetaData(byte[] file);

        /// <summary>
        /// Zwraca metadane pliku: rozmiar i hash SHA256 dla strumienia bez buforowania całej zawartości w pamięci.
        /// </summary>
        /// <param name="fileStream">Strumień pliku.</param>
        /// <returns><see cref="FileMetadata"/></returns>
        FileMetadata GetMetaData(Stream fileStream);

        /// <summary>
        /// Zwraca asynchronicznie metadane pliku: rozmiar i hash SHA256 dla strumienia bez buforowania całej zawartości w pamięci.
        /// </summary>
        /// <param name="fileStream">Strumień pliku.</param>
        /// <param name="cancellationToken">Token anulowania</param>
        /// <returns><see cref="FileMetadata"/></returns>
        Task<FileMetadata> GetMetaDataAsync(Stream fileStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Zwraca zaszyfrowany plik w formie tablicy bajtów przy użyciu algorytmu RSA.
        /// </summary>
        /// <param name="content">Niezaszyfrowany plik w formacie tablicy bajtów</param>
        /// <param name="padding">Wypełnienie</param>
        /// <returns></returns>
        byte[] EncryptWithRSAUsingPublicKey(byte[] content, RSAEncryptionPadding padding);

        /// <summary>
        /// Zwraca zaszyfrowany token KSeF przy użyciu algorytmu RSA z publicznym kluczem.
        /// </summary>
        /// <param name="content">Niezaszyfrowany plik w formacie tablicy bajtów</param>
        /// <returns></returns>
        byte[] EncryptKsefTokenWithRSAUsingPublicKey(byte[] content);

        /// <summary>
        /// Zwraca zaszyfrowany token KSeF przy użyciu algorytmu ECIes z publicznym kluczem.
        /// </summary>
        /// <param name="content">Niezaszyfrowany plik w formacie tablicy bajtów</param>
        /// <returns></returns>
        byte[] EncryptWithECDSAUsingPublicKey(byte[] content);

        /// <summary>
        /// Jednorazowe, asynchroniczne wstępne załadowanie certyfikatów i kluczy do pamięci podręcznej.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WarmupAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Wymusza odświeżenie certyfikatów i kluczy w pamięci podręcznej.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ForceRefreshAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Metoda pozwalająca na ręczne ustawienie materiałów kryptograficznych.
        /// Pomija mechanizm fetchera i odświeżania.
        /// </summary>
        void SetExternalMaterials(X509Certificate2 symmetricKeyCert, X509Certificate2 ksefTokenCert);

        /// <summary>
        /// Certyfikat używany do szyfrowania symetrycznego klucza AES.
        /// </summary>
        X509Certificate2 SymmetricKeyCertificate { get; }

        /// <summary>
        /// Certyfikat używany do szyfrowania tokena KSeF.
        /// </summary>
        X509Certificate2 KsefTokenCertificate { get; }
    }
}