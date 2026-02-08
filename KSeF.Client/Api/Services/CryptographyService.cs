using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Extensions;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class CryptographyService : ICryptographyService, IDisposable
{
    // JEDYNA zewnętrzna zależność: interfejs do pobrania listy certyfikatów
    private readonly ICertificateFetcher _fetcher;

    private readonly TimeSpan _staleGrace = TimeSpan.FromHours(6);  // przy chwilowej awarii

    // Cache
    private CertificateMaterials _materials;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Timer _refreshTimer;
    private bool _isInitialized;
    private bool _isExternallyManaged;
    private bool _disposedValue;

    /// <summary>
    /// Inicjuje nową instancję klasy <see cref="CryptographyService"/> z określonym mechanizmem pobierania certyfikatów.
    /// </summary>
    /// <param name="fetcher">Mechanizm pobierania certyfikatów używany do ich odzyskiwania na potrzeby operacji kryptograficznych. Nie może mieć wartości <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Zwracany, jeśli <paramref name="fetcher"/> ma wartość <see langword="null"/>.</exception>
    public CryptographyService(ICertificateFetcher fetcher)
    {
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
    }

    /// <summary>
    /// Inicjuje nową instancję klasy <see cref="CryptographyService"/> przy użyciu określonej funkcji pobierającej certyfikaty.
    /// </summary>
    /// <remarks>Zaleca się używanie głównego konstruktora, który
    /// akceptuje <see cref="ICertificateFetcher"/>, aby ułatwić wstrzykiwanie zależności i testowanie.</remarks>
    /// <param name="fetcher">Delegat, który asynchronicznie pobiera kolekcję obiektów <see cref="PemCertificateInfo"/>. Funkcja
    /// przyjmuje <see cref="CancellationToken"/></param>
    /// <exception cref="ArgumentNullException">Zwracany, jeśli <paramref name="fetcher"/> ma wartość <see langword="null"/>.</exception>
    [Obsolete("Zaleca się użycie głównego konstruktora z podaniem ICertificateFetcher, ułatwia to DI i testowanie.")]
    public CryptographyService(Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> fetcher)
    {
        _fetcher = new CertificateFetcher(fetcher ?? throw new ArgumentNullException(nameof(fetcher)));
    }

    /// <inheritdoc />
    public bool IsWarmedUp() => Volatile.Read(ref _materials) != null;

    /// <summary>
    /// Certyfikat używany do szyfrowania klucza symetrycznego.
    /// </summary>
    public X509Certificate2 SymmetricKeyCertificate =>
        (_materials ?? throw NotReady()).SymmetricKeyCert;

    /// <summary>
    /// Certyfikat używany do szyfrowania tokenu KSeF.
    /// </summary>
    public X509Certificate2 KsefTokenCertificate =>
        (_materials ?? throw NotReady()).KsefTokenCert;

    /// <summary>
    /// Certyfikat używany do szyfrowania klucza symetrycznego w formacie PEM.
    /// </summary>
    public string SymmetricKeyEncryptionPem => ToPem(SymmetricKeyCertificate);

    /// <summary>
    /// Certyfikat używany do szyfrowania tokenu KSeF w formacie PEM.
    /// </summary>
    public string KsefTokenPem => ToPem(KsefTokenCertificate);

    /// <inheritdoc />
    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        if (_isExternallyManaged)
        {
            return; // Nie wykonuj, jeśli zarządzane zewnętrznie
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false); // pobierz po raz pierwszy
        ScheduleNextRefresh();  // ustaw czas następnego odświeżania
    }

    /// <inheritdoc />
    public async Task ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isExternallyManaged)
        {
            return; // Nie wykonuj, jeśli zarządzane zewnętrznie
        }

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
        ScheduleNextRefresh();
    }

    /// <inheritdoc />
    public void SetExternalMaterials(X509Certificate2 symmetricKeyCert, X509Certificate2 ksefTokenCert)
    {
        Guard.ThrowIfNull(symmetricKeyCert);
        Guard.ThrowIfNull(ksefTokenCert);

        _refreshTimer?.Dispose(); // Wyłącz automatyczne odświeżanie
        _isExternallyManaged = true; // Oznacz jako zarządzane zewnętrznie

        // Tworzy materiały bez daty wygaśnięcia i odświeżania
        CertificateMaterials materials = new(symmetricKeyCert, ksefTokenCert, DateTimeOffset.MaxValue, DateTimeOffset.MaxValue);
        Volatile.Write(ref _materials, materials);
        _isInitialized = true; // Oznacz jako zainicjalizowane
    }

    /// <inheritdoc />
    public EncryptionData GetEncryptionData()
    {
        byte[] key = GenerateRandom256BitsKey();
        byte[] iv = GenerateRandom16BytesIv();

        byte[] encryptedKey = EncryptWithRSAUsingPublicKey(key, RSAEncryptionPadding.OaepSHA256);
        EncryptionInfo encryptionInfo = new()
        {
            EncryptedSymmetricKey = Convert.ToBase64String(encryptedKey),

            InitializationVector = Convert.ToBase64String(iv)
        };
        return new EncryptionData
        {
            CipherKey = key,
            CipherIv = iv,
            EncryptionInfo = encryptionInfo
        };
    }

    /// <inheritdoc />
    public byte[] EncryptBytesWithAES256(byte[] content, byte[] key, byte[] iv)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        using Stream input = BinaryData.FromBytes(content).ToStream();
        using MemoryStream output = new();
        using CryptoStream cryptoWriter = new(output, encryptor, CryptoStreamMode.Write);

        input.CopyTo(cryptoWriter);
        cryptoWriter.FlushFinalBlock();
        output.Position = 0;

        return BinaryData.FromStream(output).ToArray();
    }

    /// <inheritdoc />
    public void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform encryptor = aes.CreateEncryptor();
#if NETSTANDARD2_0
        // CryptoStream(stream, transform, mode, leaveOpen) not available on netstandard2.0.
        // Don't use 'using' — Dispose would close the output stream.
        CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write);
        input.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();
#else
        using CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write, leaveOpen: true);

        input.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();
#endif

        if (output.CanSeek)
        {
            output.Position = 0;
        }
    }

    /// <inheritdoc />
    public async Task EncryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform encryptor = aes.CreateEncryptor();
#if NETSTANDARD2_0
        CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write);
        await input.CopyToAsync(cryptoStream, 81920, cancellationToken).ConfigureAwait(false);
        cryptoStream.FlushFinalBlock();
#else
        using CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write, leaveOpen: true);

        await input.CopyToAsync(cryptoStream, 81920, cancellationToken).ConfigureAwait(false);
        await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);
#endif

        if (output.CanSeek)
        {
            output.Position = 0;
        }
    }

    /// <inheritdoc />
    public byte[] DecryptBytesWithAES256(byte[] content, byte[] key, byte[] iv)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using Stream input = BinaryData.FromBytes(content).ToStream();
        using MemoryStream output = new();
        using CryptoStream cryptoReader = new(input, decryptor, CryptoStreamMode.Read);

        cryptoReader.CopyTo(output);
        output.Position = 0;

        return BinaryData.FromStream(output).ToArray();
    }

    /// <inheritdoc />
    public void DecryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using CryptoStream cryptoStream = new(input, decryptor, CryptoStreamMode.Read);

        cryptoStream.CopyTo(output);

        if (output.CanSeek)
        {
            output.Position = 0;
        }
    }

    /// <inheritdoc />
    public async Task DecryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken cancellationToken = default)
    {
        using Aes aes = CreateConfiguredAes(key, iv);
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using CryptoStream cryptoStream = new(input, decryptor, CryptoStreamMode.Read);

        await cryptoStream.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);

        if (output.CanSeek)
        {
            output.Position = 0;
        }
    }

    /// <inheritdoc />
    public (string, string) GenerateCsrWithRsa(CertificateEnrollmentsInfoResponse certificateInfo, RSASignaturePadding padding = null)
    {
#if NETSTANDARD2_0
        // CertificateRequest is not available on netstandard2.0.
        throw new PlatformNotSupportedException(
            "Generowanie CSR (CertificateRequest) nie jest obsługiwane na platformie netstandard2.0/.NET Framework. " +
            "Użyj .NET 8+ do operacji rejestracji certyfikatów.");
#else
        if (padding == null)
        {
            padding = RSASignaturePadding.Pss;
        }

        using RSA rsa = RSA.Create(2048);
        byte[] privateKey = rsa.ExportRSAPrivateKey();

        X500DistinguishedName subject = CreateSubjectDistinguishedName(certificateInfo);

        CertificateRequest request = new(subject, rsa, HashAlgorithmName.SHA256, padding);

        byte[] csrDer = request.CreateSigningRequest();
        return (Convert.ToBase64String(csrDer), Convert.ToBase64String(privateKey));
#endif
    }

    /// <inheritdoc />
    public FileMetadata GetMetaData(byte[] file)
    {
#if NETSTANDARD2_0
        byte[] hash = HashCompat.SHA256HashData(file);
#else
        byte[] hash = SHA256.HashData(file);
#endif
        string base64Hash = Convert.ToBase64String(hash);

        int fileSize = file.Length;

        return new FileMetadata
        {
            FileSize = fileSize,
            HashSHA = base64Hash
        };
    }

    /// <inheritdoc />
    public FileMetadata GetMetaData(Stream fileStream)
    {
        Guard.ThrowIfNull(fileStream);

        long originalPosition = 0;
        bool restorePosition = false;
        long fileSize;

        if (fileStream.CanSeek)
        {
            originalPosition = fileStream.Position;
            fileStream.Position = 0;
            restorePosition = true;
            fileSize = fileStream.Length;
        }
        else
        {
            fileSize = 0;
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] buffer = new byte[81920];
        int read;
        while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            sha256.TransformBlock(buffer, 0, read, null, 0);
            if (!fileStream.CanSeek)
            {
                fileSize += read;
            }
        }
        sha256.TransformFinalBlock([], 0, 0);

        string base64Hash = Convert.ToBase64String(sha256.Hash!);

        if (restorePosition)
        {
            fileStream.Position = originalPosition;
        }

        return new FileMetadata
        {
            FileSize = fileSize,
            HashSHA = base64Hash
        };
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetaDataAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(fileStream);

        long originalPosition = 0;
        bool restorePosition = false;
        long fileSize;

        if (fileStream.CanSeek)
        {
            originalPosition = fileStream.Position;
            fileStream.Position = 0;
            restorePosition = true;
            fileSize = fileStream.Length;
        }
        else
        {
            fileSize = 0;
        }

        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        int read;
#if NETSTANDARD2_0
        while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
#else
        while ((read = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
#endif
        {
            hasher.AppendData(buffer, 0, read);
            if (!fileStream.CanSeek)
            {
                fileSize += read;
            }
        }

        string base64Hash = Convert.ToBase64String(hasher.GetHashAndReset());

        if (restorePosition)
        {
            fileStream.Position = originalPosition;
        }

        return new FileMetadata
        {
            FileSize = fileSize,
            HashSHA = base64Hash
        };
    }

    /// <inheritdoc />
    public byte[] EncryptWithRSAUsingPublicKey(byte[] content, RSAEncryptionPadding padding)
    {
        RSA rsa = RSA.Create();
        string publicKey = GetRSAPublicPem(SymmetricKeyEncryptionPem);
        rsa.ImportFromPem(publicKey);
        return rsa.Encrypt(content, padding);
    }

    /// <inheritdoc />
    public byte[] EncryptKsefTokenWithRSAUsingPublicKey(byte[] content)
    {
        RSA rsa = RSA.Create();
        string publicKey = GetRSAPublicPem(KsefTokenPem);
        rsa.ImportFromPem(publicKey);
        return rsa.Encrypt(content, RSAEncryptionPadding.OaepSHA256);
    }

    /// <inheritdoc />
    public byte[] EncryptWithECDSAUsingPublicKey(byte[] content)
    {
#if NETSTANDARD2_0
        // ECDiffieHellman and AesGcm are not available on netstandard2.0.
        throw new PlatformNotSupportedException(
            "Szyfrowanie ECDH+AES-GCM nie jest obsługiwane na platformie netstandard2.0/.NET Framework. " +
            "Użyj .NET 8+ do szyfrowania tokenem KSeF z kluczem ECDSA.");
#else
        using ECDiffieHellman ecdhReceiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        string publicKey = GetECDSAPublicPem(KsefTokenPem);
        ecdhReceiver.ImportFromPem(publicKey);

        using ECDiffieHellman ecdhEphemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        byte[] sharedSecret = ecdhEphemeral.DeriveKeyMaterial(ecdhReceiver.PublicKey);

        using AesGcm aes = new(sharedSecret, AesGcm.TagByteSizes.MaxSize);
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        byte[] cipherText = new byte[content.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt(nonce, content, cipherText, tag);

        byte[] subjectPublicKeyInfo = ecdhEphemeral.PublicKey.ExportSubjectPublicKeyInfo();
        return [.. subjectPublicKeyInfo
, .. nonce, .. tag, .. cipherText];
#endif
    }
    /// <summary>
    /// Zapewnia funkcjonalność do asynchronicznego pobierania kolekcji informacji o certyfikatach PEM.
    /// </summary>
    /// <remarks>Ta klasa jest implementacją interfejsu <see cref="ICertificateFetcher"/>,
    /// zaprojektowaną do pobierania certyfikatów przy użyciu określonej funkcji asynchronicznej.
    /// Służy wyłącznie utrzymaniu kompatybilności wstecznej (użyciu konstruktora z delegatem)</remarks>
    private sealed class CertificateFetcher(Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> func) : ICertificateFetcher
    {   
        public Task<ICollection<PemCertificateInfo>> GetCertificatesAsync(CancellationToken cancellationToken) => func(cancellationToken);
    }

    private static Aes CreateConfiguredAes(byte[] key, byte[] iv)
    {
        Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 16 * 8;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }

    private static byte[] GenerateRandom256BitsKey()
    {
        byte[] key = new byte[256 / 8];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        return key;
    }

    private static byte[] GenerateRandom16BytesIv()
    {
        byte[] iv = new byte[16];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);

        return iv;
    }

    private static string GetRSAPublicPem(string certificatePem)
    {
#if NETSTANDARD2_0
        X509Certificate2 cert = PemHelper.CreateCertificateFromPem(certificatePem);
#else
        X509Certificate2 cert = X509Certificate2.CreateFromPem(certificatePem);
#endif

        RSA rsa = cert.GetRSAPublicKey();
        if (rsa != null)
        {
            string pubKeyPem = ExportPublicKeyToPem(rsa);
            return pubKeyPem;
        }
        else
        {
            throw new InvalidOperationException("Nie znaleziono klucza RSA.");
        }
    }

    private static string GetECDSAPublicPem(string certificatePem)
    {
#if NETSTANDARD2_0
        X509Certificate2 cert = PemHelper.CreateCertificateFromPem(certificatePem);
#else
        X509Certificate2 cert = X509Certificate2.CreateFromPem(certificatePem);
#endif

        ECDsa ecdsa = cert.GetECDsaPublicKey();
        if (ecdsa != null)
        {
            string pubKeyPem = ExportEcdsaPublicKeyToPem(ecdsa);
            return pubKeyPem;
        }
        else
        {
            throw new InvalidOperationException("Nie znaleziono klucza ECDSA.");
        }
    }

    private static string ExportEcdsaPublicKeyToPem(ECDsa ecdsa)
    {
        byte[] pubKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
#if NETSTANDARD2_0
        return PemHelper.EncodePem("PUBLIC KEY", pubKeyBytes);
#else
        return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
#endif
    }

    private static string ExportPublicKeyToPem(RSA rsa)
    {
        byte[] pubKeyBytes = rsa.ExportSubjectPublicKeyInfo();
#if NETSTANDARD2_0
        return PemHelper.EncodePem("PUBLIC KEY", pubKeyBytes);
#else
        return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
#endif
    }

    private static string ToPem(X509Certificate2 certificate) =>
    "-----BEGIN CERTIFICATE-----\n" +
    Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) +
    "\n-----END CERTIFICATE-----";

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized || _isExternallyManaged)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            ICollection<PemCertificateInfo> list = await _fetcher.GetCertificatesAsync(cancellationToken).ConfigureAwait(false);
            CertificateMaterials certificateMaterials = BuildMaterials(list);

            Volatile.Write(ref _materials, certificateMaterials);

            _isInitialized = true;
        }
        catch
        {
            CertificateMaterials current = Volatile.Read(ref _materials);
            if (current is null || DateTimeOffset.UtcNow > current.ExpiresAt + _staleGrace)
            {
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ScheduleNextRefresh()
    {
        if (_isExternallyManaged)
        {
            return;
        }

        CertificateMaterials certificateMaterials = Volatile.Read(ref _materials)!;
        if (certificateMaterials is null)
        {
            return;
        }

        TimeSpan due = certificateMaterials.RefreshAt - DateTimeOffset.UtcNow;
        if (due < TimeSpan.FromSeconds(5))
        {
            due = TimeSpan.FromSeconds(5);
        }

        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(async _ =>
        {
            try
            {
                _isInitialized = false;
                await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                // po udanym (lub łagodnie nieudanym) odświeżeniu ustaw kolejny termin
                ScheduleNextRefresh();
            }
        }, null, due, Timeout.InfiniteTimeSpan);
    }

    private static CertificateMaterials BuildMaterials(ICollection<PemCertificateInfo> certs)
    {
        if (certs.Count == 0)
        {
            throw new InvalidOperationException("Brak certyfikatów.");
        }

        PemCertificateInfo symmetricDto = certs.FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.SymmetricKeyEncryption))
            ?? throw new InvalidOperationException("Brak certyfikatu SymmetricKeyEncryption.");
        PemCertificateInfo tokenDto = certs.OrderBy(c => c.ValidFrom)
            .FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.KsefTokenEncryption))
            ?? throw new InvalidOperationException("Brak certyfikatu KsefTokenEncryption.");

        byte[] symetricBytes = Convert.FromBase64String(symmetricDto.Certificate);
        X509Certificate2 sym = symetricBytes.LoadCertificate();
        byte[] tokenBytes = Convert.FromBase64String(tokenDto.Certificate);
        X509Certificate2 tok = tokenBytes.LoadCertificate();

        DateTime minNotAfterUtc = new[] { sym.NotAfter.ToUniversalTime(), tok.NotAfter.ToUniversalTime() }.Min();
        DateTimeOffset expiresAt = new(minNotAfterUtc, TimeSpan.Zero);

        // odśwież przed wygaśnięciem lub najpóźniej za maxRevalidateInterval
        TimeSpan safetyMargin = TimeSpan.FromDays(1);
        TimeSpan maxRevalidateInterval = TimeSpan.FromHours(24);

        DateTimeOffset refreshCandidate = expiresAt - safetyMargin;
        DateTimeOffset capByMaxInterval = DateTimeOffset.UtcNow + maxRevalidateInterval;
        DateTimeOffset refreshAt = (refreshCandidate < capByMaxInterval) ? refreshCandidate : capByMaxInterval;

        // drobny jitter 0–5 min, by nie wstały wszystkie instancje naraz
#if NETSTANDARD2_0
        refreshAt -= TimeSpan.FromMinutes(RandomCompat.Shared.Next(0, 5));
#else
        refreshAt -= TimeSpan.FromMinutes(Random.Shared.Next(0, 5));
#endif

        return new CertificateMaterials(sym, tok, expiresAt, refreshAt);
    }

    private static InvalidOperationException NotReady() =>
        new("Materiały kryptograficzne nie są jeszcze zainicjalizowane. " +
            "Wywołaj WarmupAsync() na starcie aplikacji lub ForceRefreshAsync().");

    /// <inheritdoc />
    public (string, string) GenerateCsrWithEcdsa(CertificateEnrollmentsInfoResponse certificateInfo)
    {
#if NETSTANDARD2_0
        throw new PlatformNotSupportedException(
            "Generowanie CSR (CertificateRequest) nie jest obsługiwane na platformie netstandard2.0/.NET Framework. " +
            "Użyj .NET 8+ do operacji rejestracji certyfikatów.");
#else
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = ecdsa.ExportECPrivateKey();

        X500DistinguishedName subject = CreateSubjectDistinguishedName(certificateInfo);

        // Budowanie CSR
        CertificateRequest request = new(subject, ecdsa, HashAlgorithmName.SHA256);

        // Eksport CSR do formatu DER (bajtów)
        byte[] csrDer = request.CreateSigningRequest();
        return (Convert.ToBase64String(csrDer), Convert.ToBase64String(privateKey));
#endif
    }

    private static X500DistinguishedName CreateSubjectDistinguishedName(CertificateEnrollmentsInfoResponse certificateInfo)
    {
        AsnWriter asnWriter = new(AsnEncodingRules.DER);

        void AddRdn(string oid, string value, UniversalTagNumber tag)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            using AsnWriter.Scope set = asnWriter.PushSetOf();
            using AsnWriter.Scope seq = asnWriter.PushSequence();
            asnWriter.WriteObjectIdentifier(oid);
            asnWriter.WriteCharacterString(tag, value);
        }

        using (asnWriter.PushSequence())
        {
            AddRdn("2.5.4.3", certificateInfo.CommonName, UniversalTagNumber.UTF8String);
            AddRdn("2.5.4.4", certificateInfo.Surname, UniversalTagNumber.UTF8String);
            AddRdn("2.5.4.42", certificateInfo.GivenName, UniversalTagNumber.UTF8String);
            AddRdn("2.5.4.10", certificateInfo.OrganizationName, UniversalTagNumber.UTF8String);
            AddRdn("2.5.4.97", certificateInfo.OrganizationIdentifier, UniversalTagNumber.UTF8String);
            AddRdn("2.5.4.6", certificateInfo.CountryName, UniversalTagNumber.PrintableString);
            AddRdn("2.5.4.5", certificateInfo.SerialNumber, UniversalTagNumber.PrintableString);
            AddRdn("2.5.4.45", certificateInfo.UniqueIdentifier, UniversalTagNumber.UTF8String);
        }

        return new X500DistinguishedName(asnWriter.Encode());
    }

    #region Implementacja IDisposable

    /// <summary>
    /// Zwalnia wszystkie zasoby używane przez bieżącą instancję klasy.
    /// </summary>
    /// <remarks>Ta metoda powinna być wywoływana, gdy instancja nie jest już potrzebna, aby zwolnić zasoby. Pomija ona finalizację w celu optymalizacji odśmiecania pamięci.</remarks>
    public void Dispose()
    {
        if (_disposedValue)
        {
            return;
        }

        _refreshTimer?.Dispose();
        _gate.Dispose();

        CertificateMaterials materials = Volatile.Read(ref _materials);
        materials?.SymmetricKeyCert.Dispose();
        materials?.KsefTokenCert.Dispose();

        _disposedValue = true;
        GC.SuppressFinalize(this);
    }

    #endregion

    private sealed record CertificateMaterials(
    X509Certificate2 SymmetricKeyCert,
    X509Certificate2 KsefTokenCert,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshAt);

    void IDisposable.Dispose()
    {
        _gate?.Dispose();

        _refreshTimer?.Dispose();

        _materials = null;

        GC.SuppressFinalize(this);
    }
}