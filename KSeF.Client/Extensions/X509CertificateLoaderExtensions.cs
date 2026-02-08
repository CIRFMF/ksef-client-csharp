using System.Runtime.InteropServices;
using System.Security.Cryptography;
#if !NETSTANDARD2_0
using System.Security.Cryptography.Pkcs;
#endif
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Extensions
{
    public static class X509CertificateLoaderExtensions
    {
        private const string RsaOid = "1.2.840.113549.1.1.1";
        private const string EcOid = "1.2.840.10045.2.1";

        private static X509KeyStorageFlags GetFlags()
        {
            // macOS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return X509KeyStorageFlags.Exportable;
            }

#if NETSTANDARD2_0
            // EphemeralKeySet is not available on netstandard2.0
            return X509KeyStorageFlags.Exportable;
#else
            // Windows / Linux
            return X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;
#endif
        }

        public static X509Certificate2 LoadCertificate(this byte[] certBytes)
        {
            Guard.ThrowIfNull(certBytes);

#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(certBytes);
#else
            return new X509Certificate2(
                certBytes,
                "",
                GetFlags());
#endif
        }

        public static X509Certificate2 LoadPkcs12(this byte[] certBytes)
        {
            Guard.ThrowIfNull(certBytes);

#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(
                certBytes,
                password: string.Empty,
                GetFlags());
#else
            return new X509Certificate2(
                certBytes,
                string.Empty,
                GetFlags());
#endif
        }

        public static X509Certificate2 LoadCertificateFromFile(string certificatePath)
        {
            Guard.ThrowIfNull(certificatePath);

#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificateFromFile(certificatePath);
#else
            byte[] certBytes = File.ReadAllBytes(certificatePath);

            return new X509Certificate2(
                certBytes,
                "",
                GetFlags());
#endif
        }

        /// <summary>
        /// Łączy publiczny certyfikat X.509 z kluczem prywatnym zakodowanym w formacie PEM w celu utworzenia certyfikatu zawierającego
        /// klucz prywatny. Obsługuje klucze typu RSA i ECDSA.
        /// </summary>
        /// <remarks>Zwracany certyfikat jest kopią oryginału z dołączonym kluczem prywatnym.
        /// Obsługiwane są tylko algorytmy RSA i ECDSA. Metoda nie modyfikuje oryginalnej instancji certyfikatu. 
        /// Metoda obsługuje następujące nagłówki PEM: PRIVATE KEY, EC PRIVATE KEY, RSA PRIVATE KEY, ENCRYPTED PRIVATE KEY.
        /// Dla szyfrowanych kluczy wspierany jest tylko standard PKCS#8 (ENCRYPTED PRIVATE KEY).</remarks>
        /// <param name="publicCert">Publiczna instancja X509Certificate2, do której zostanie dołączony klucz prywatny. Nie może zawierać już
        /// klucza prywatnego.</param>
        /// <param name="privateKeyPem">Ciąg znaków zawierający klucz prywatny zakodowany w formacie PEM. Może być zaszyfrowany lub niezaszyfrowany. Nie może być null, pusty ani
        /// zawierać spacji.</param>
        /// <param name="password">Hasło używane do odszyfrowania klucza prywatnego, jeśli format PEM jest zaszyfrowany. Jeśli klucz nie jest zaszyfrowany, ten
        /// parametr jest ignorowany.</param>
        /// <returns>Nowa instancja X509Certificate2 zawierająca zarówno certyfikat publiczny, jak i powiązany klucz prywatny. </returns>
        /// <exception cref="ArgumentNullException">Rzucane, jeśli <paramref name="publicCert"/> jest null lub jeśli <paramref name="privateKeyPem"/> jest null, puste lub
        /// składa się wyłącznie ze spacji. </exception>
        /// <exception cref="InvalidOperationException">Rzucane, jeśli <paramref name="publicCert"/> zawiera już klucz prywatny. </exception>
        /// <exception cref="NotSupportedException">Rzucane, jeśli algorytm klucza publicznego certyfikatu nie jest RSA lub ECDSA lub jeśli brakuje identyfikatora OID klucza publicznego. </exception>
        /// <exception cref="ArgumentException">Rzucane, jeśli klucz prywatny PEM jest zaszyfrowany, a <paramref name="password"/> jest pusty lub ma wartość null. </exception>
        public static X509Certificate2 MergeWithPemKey(
            this X509Certificate2 publicCert,
            string privateKeyPem,
            string password = null)
        {
            Guard.ThrowIfNull(publicCert);
            if (string.IsNullOrWhiteSpace(privateKeyPem))
            {
                throw new ArgumentNullException(nameof(privateKeyPem));
            }

            if (publicCert.HasPrivateKey)
            {
                throw new InvalidOperationException("Certyfikat zawiera już klucz prywatny.");
            }

            string oid = publicCert.PublicKey.Oid?.Value
                ?? throw new NotSupportedException("Certyfikat nie zawiera klucza publicznego OID.");

#if NETSTANDARD2_0
            bool isEncrypted = privateKeyPem.Contains("ENCRYPTED PRIVATE KEY");
#else
            bool isEncrypted = privateKeyPem.Contains(
                "ENCRYPTED PRIVATE KEY",
                StringComparison.OrdinalIgnoreCase);
#endif

            if (isEncrypted && string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(
                    "Zaszyfrowany klucz prywatny wymaga podania hasła.",
                    nameof(password));
            }

            if (oid == RsaOid)
            {
                using RSA rsa = RSA.Create();
                if (isEncrypted)
                {
                    rsa.ImportFromEncryptedPem(privateKeyPem, password);
                }
                else
                {
                    rsa.ImportFromPem(privateKeyPem);
                }

                return publicCert.CopyWithPrivateKey(rsa);
            }
            else if (oid == EcOid)
            {
                using ECDsa ecdsa = ECDsa.Create();

#if !NETSTANDARD2_0
                try
                {
#endif
                    if (isEncrypted)
                    {
                        ecdsa.ImportFromEncryptedPem(privateKeyPem, password);
                    }
                    else
                    {
                        ecdsa.ImportFromPem(privateKeyPem);
                    }
#if !NETSTANDARD2_0
                }
                catch (CryptographicException ex) when (isEncrypted && IsPkcs8PasswordError(ex))
                {
                    return publicCert.MergeWithPemKeyNoProfileForEcdsa(privateKeyPem, password);
                }
#endif

                return publicCert.CopyWithPrivateKey(ecdsa);
            }

            throw new NotSupportedException(
                $"Algorytym o OID '{oid}' nie jest wspierany.");
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Bezpiecznie łączy publiczny certyfikat ECDSA X.509 z zaszyfrowanym kluczem prywatnym w formacie PKCS#8 (PEM),
        /// nie wymagając profilu użytkownika w systemie.
        /// </summary>
        /// <remarks> Metoda jest przeznaczona do użycia w specyficznych scenariuszach, w których:
        /// - używany jest certyfikat ECDSA,
        /// - klucz prywatny jest zaszyfrowany i zapisany w formacie PKCS#8 jako blok ENCRYPTED PRIVATE KEY w PEM,
        /// - środowisko wykonania nie udostępnia profilu użytkownika (np. IIS z wyłączonym LoadUserProfile) i standardowy import klucza może kończyć się błędem.
        /// Użycie tej metody pozwala pominąć platformowe ograniczenia związane z CNG i ręcznie odszyfrować klucz w pamięci, co poprawia niezawodność w takich środowiskach.
        /// Ze względów wydajnościowych zaleca się wywoływać tę metodę bezpośrednio tylko wtedy, gdy wiadomo, że powyższe warunki są spełnione. W typowych scenariuszach
        /// należy używać metody <c>MergeWithPemKey</c>, która korzysta z wbudowanych mechanizmów importu kluczy, a dopiero w przypadku problemów (np. konkretnych
        /// wyjątków kryptograficznych) uruchamia wewnętrznie ścieżkę opartą o <c>MergeWithPemKeyNoProfileForEcdsa</c>.
        /// </remarks>
        /// <param name="publicCert"> Publiczny certyfikat X.509 zawierający klucz ECDSA, który ma zostać powiązany z kluczem prywatnym.</param>
        /// <param name="privateKeyPem">Treść zaszyfrowanego klucza prywatnego w formacie PEM (blok „ENCRYPTED PRIVATE KEY" PKCS#8).</param>
        /// <param name="password"> Hasło użyte do zaszyfrowania klucza prywatnego PKCS#8.</param>
        /// <returns> Nowy obiekt <see cref="X509Certificate2"/> zawierający zarówno certyfikat publiczny,
        /// jak i odpowiadający mu klucz prywatny ECDSA, załadowany wyłącznie w pamięci. </returns>
        /// <exception cref="ArgumentNullException"> Rzucany, gdy <paramref name="publicCert"/> lub <paramref name="privateKeyPem"/> jest puste.</exception>
        /// <exception cref="ArgumentException"> Rzucany, gdy <paramref name="password"/> jest puste lub ma wartość null.</exception>
        /// <exception cref="CryptographicException"> Rzucany, gdy dane PEM są nieprawidłowe, nie mogą zostać poprawnie odszyfrowane
        /// lub zdekodowany klucz PKCS#8 nie został w całości przetworzony.</exception>
        /// <exception cref="NotSupportedException">Rzucany, gdy zdekodowany klucz PKCS#8 nie reprezentuje klucza ECDSA.</exception>
        public static X509Certificate2 MergeWithPemKeyNoProfileForEcdsa(this X509Certificate2 publicCert, string privateKeyPem, string password)
        {
            Guard.ThrowIfNull(publicCert);

            if (string.IsNullOrWhiteSpace(privateKeyPem))
            {
                throw new ArgumentNullException(nameof(privateKeyPem));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(
                    "Szyfrowany klucz prywatny ECDSA wymaga podania hasła.",
                    nameof(password));
            }

            using ECDsa ecdsa = ECDsa.Create();

            byte[] encryptedPkcs8 = ExtractEncryptedPkcs8FromPem(privateKeyPem);

            int bytesRead;
            Pkcs8PrivateKeyInfo privateKeyInfo = Pkcs8PrivateKeyInfo.DecryptAndDecode(
                password.AsSpan(),
                encryptedPkcs8,
                out bytesRead);

            if (bytesRead != encryptedPkcs8.Length)
            {
                throw new CryptographicException(
                    $"Nie udało się całkowicie zaimportować {nameof(Pkcs8PrivateKeyInfo)}. BytesRead={bytesRead}, Total={encryptedPkcs8.Length}.");
            }

            if (!IsEcdsaPrivateKey(privateKeyInfo))
            {
                throw new NotSupportedException(
                    $"Odszyfrowany klucz PKCS#8 nie jest poprawnym kluczem ECDSA. OID algorytmu: {privateKeyInfo.AlgorithmId.Value}.");
            }

            ReadOnlyMemory<byte> pkcs8Der = privateKeyInfo.Encode();

            int importedBytes;
            ecdsa.ImportPkcs8PrivateKey(pkcs8Der.Span, out importedBytes);

            if (importedBytes != pkcs8Der.Length)
            {
                throw new CryptographicException(
                    $"Nie udało się całkowicie zaimportować {nameof(Pkcs8PrivateKeyInfo)}. BytesRead={importedBytes}, Total={pkcs8Der.Length}.");
            }
            return publicCert.CopyWithPrivateKey(ecdsa);
        }

        private static bool IsPkcs8PasswordError(CryptographicException cryptographyException)
        {
            if (cryptographyException is null)
            {
                return false;
            }

            string message = cryptographyException.Message ?? string.Empty;

            return message.Contains("EncryptedPrivateKeyInfo", StringComparison.OrdinalIgnoreCase)
                || message.Contains("password may be incorrect", StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] ExtractEncryptedPkcs8FromPem(string pem)
        {
            Guard.ThrowIfNull(pem);

            const string Begin = "-----BEGIN ENCRYPTED PRIVATE KEY-----";
            const string End = "-----END ENCRYPTED PRIVATE KEY-----";

            int begin = pem.IndexOf(Begin, StringComparison.OrdinalIgnoreCase);
            int end = pem.IndexOf(End, StringComparison.OrdinalIgnoreCase);

            if (begin < 0 || end < 0 || end <= begin)
            {
                throw new CryptographicException("PEM nie zawiera poprawnego bloku ENCRYPTED PRIVATE KEY.");
            }

            int base64Start = begin + Begin.Length;
            string base64 = pem.Substring(base64Start, end - base64Start);

            string normalized = new(base64.Where(c => !char.IsWhiteSpace(c)).ToArray());

            try
            {
                return Convert.FromBase64String(normalized);
            }
            catch (FormatException formatException)
            {
                throw new CryptographicException("PEM ENCRYPTED PRIVATE KEY zawiera nieprawidłowe dane Base64.", formatException);
            }
        }

        private static bool IsEcdsaPrivateKey(Pkcs8PrivateKeyInfo privateKeyInfo)
        {
            Guard.ThrowIfNull(privateKeyInfo);

            string algorithmOid = privateKeyInfo.AlgorithmId?.Value ?? string.Empty;

            return string.Equals(algorithmOid, EcOid, StringComparison.Ordinal);
        }
#endif

    }
}