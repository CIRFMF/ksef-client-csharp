using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class CryptographyService : ICryptographyService
{
    // JEDYNA zewnętrzna zależność: delegat do pobrania listy certów
    private readonly Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> _fetcher;

    private readonly TimeSpan _staleGrace = TimeSpan.FromHours(6);  // przy chwilowej awarii

    // Cache
    private CertificateMaterials _materials;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Timer _refreshTimer;

    public CryptographyService(
        Func<CancellationToken, Task<ICollection<PemCertificateInfo>>> fetcher)
    {
        _fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
    }

    public X509Certificate2 SymmetricKeyCertificate =>
        (_materials ?? throw NotReady()).SymmetricKeyCert;

    public X509Certificate2 KsefTokenCertificate =>
        (_materials ?? throw NotReady()).KsefTokenCert;

    public string SymmetricKeyEncryptionPem => ToPem(SymmetricKeyCertificate);
    public string KsefTokenPem => ToPem(KsefTokenCertificate);

    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken); // pobierz po raz pierwszy
        ScheduleNextRefresh();  // ustaw timer
    }

    public async Task ForceRefreshAsync(CancellationToken cancellationTokem = default)
    {
        await RefreshAsync(cancellationTokem);
        ScheduleNextRefresh();
    }
    
    /// <inheritdoc />
    public EncryptionData GetEncryptionData()
    {
        byte[] key = GenerateRandom256BitsKey();
        byte[] iv = GenerateRandom16BytesIv();

        byte[] encryptedKey = EncryptWithRSAUsingPublicKey(key, RSAEncryptionPadding.OaepSHA256);
        EncryptionInfo encryptionInfo = new EncryptionInfo()
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
        Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 16 * 8;
        aes.Key = key;
        aes.IV = iv;

        ICryptoTransform encryptor = aes.CreateEncryptor();

        using Stream input = BinaryData.FromBytes(content).ToStream();
        using MemoryStream output = new();
        using CryptoStream cryptoWriter = new(output, encryptor, CryptoStreamMode.Write);
        input.CopyTo(cryptoWriter);
        cryptoWriter.FlushFinalBlock();

        output.Position = 0;
        return BinaryData.FromStream(output).ToArray();
    }

    public void EncryptStreamWithAES256(Stream input, Stream output, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 16 * 8;
        aes.Key = key;
        aes.IV = iv;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        using CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        input.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();
        output.Position = 0;
    }

    /// <inheritdoc />
    public async Task EncryptStreamWithAES256Async(Stream input, Stream output, byte[] key, byte[] iv, CancellationToken ct = default)
    {
        using Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 16 * 8;
        aes.Key = key;
        aes.IV = iv;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        using CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        await input.CopyToAsync(cryptoStream, 81920, ct).ConfigureAwait(false);
        await cryptoStream.FlushFinalBlockAsync(ct).ConfigureAwait(false);
        if (output.CanSeek)
            output.Position = 0;
    }

    /// <inheritdoc />
    public (string, string) GenerateCsrWithRsa(CertificateEnrollmentsInfoResponse certificateInfo, RSASignaturePadding padding = null)
    {
        if(padding == null)
            padding = RSASignaturePadding.Pss;

        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKey();

        X500DistinguishedName subject = CreateSubjectDistinguishedName(certificateInfo);

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, padding);

        var csrDer = request.CreateSigningRequest();
        return (Convert.ToBase64String(csrDer), Convert.ToBase64String(privateKey));
    }

    /// <inheritdoc />
    public FileMetadata GetMetaData(byte[] file)
    {
        string base64Hash = "";
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(file);
            base64Hash = Convert.ToBase64String(hash);
        }

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
        ArgumentNullException.ThrowIfNull(fileStream);

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

        using var sha256 = SHA256.Create();
        var buffer = new byte[81920];
        int read;
        while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            sha256.TransformBlock(buffer, 0, read, null, 0);
            if (!fileStream.CanSeek)
                fileSize += read;
        }
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        string base64Hash = Convert.ToBase64String(sha256.Hash!);

        if (restorePosition)
            fileStream.Position = originalPosition;

        return new FileMetadata
        {
            FileSize = fileSize,
            HashSHA = base64Hash
        };
    }

    public async Task<FileMetadata> GetMetaDataAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

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

        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        int read;
        while ((read = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            hasher.AppendData(buffer, 0, read);
            if (!fileStream.CanSeek)
                fileSize += read;
        }

        string base64Hash = Convert.ToBase64String(hasher.GetHashAndReset());

        if (restorePosition)
            fileStream.Position = originalPosition;

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
        var publicKey = GetRSAPublicPem(SymmetricKeyEncryptionPem);
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
        using ECDiffieHellman ecdhReceiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        string publicKey = GetECDSAPublicPem(KsefTokenPem);
        ecdhReceiver.ImportFromPem(publicKey);

        using ECDiffieHellman ecdhEphemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var sharedSecret = ecdhEphemeral.DeriveKeyMaterial(ecdhReceiver.PublicKey);

        using AesGcm aes = new(sharedSecret, AesGcm.TagByteSizes.MaxSize);
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        byte[] cipherText = new byte[content.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt(nonce, content, cipherText, tag);

        byte[] subjectPublicKeyInfo = ecdhEphemeral.PublicKey.ExportSubjectPublicKeyInfo();
        return subjectPublicKeyInfo
            .Concat(nonce)
            .Concat(tag)
            .Concat(cipherText)
            .ToArray();
    }

    private byte[] GenerateRandom256BitsKey()
    {
        byte[] key = new byte[256 / 8];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        return key;
    }

    private byte[] GenerateRandom16BytesIv()
    {
        byte[] iv = new byte[16];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);

        return iv;
    }

    private string GetRSAPublicPem(string certificatePem)
    {
        var cert = X509Certificate2.CreateFromPem(certificatePem);

        var rsa = cert.GetRSAPublicKey();
        if (rsa != null)
        {
            string pubKeyPem = ExportPublicKeyToPem(rsa);
            return pubKeyPem;
        }
        else
        {
            throw new Exception("Nie znaleziono klucza RSA.");
        }
    }

    private string GetECDSAPublicPem(string certificatePem)
    {
        var cert = X509Certificate2.CreateFromPem(certificatePem);

        var ecdsa = cert.GetECDsaPublicKey();
        if (ecdsa != null)
        {
            string pubKeyPem = ExportEcdsaPublicKeyToPem(ecdsa);
            return pubKeyPem;
        }
        else
        {
            throw new Exception("Nie znaleziono klucza ECDSA.");
        }
    }

    private string ExportEcdsaPublicKeyToPem(ECDsa ecdsa)
    {
        var pubKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
    }

    private string ExportPublicKeyToPem(RSA rsa)
    {
        var pubKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
    }

    private string ToPem(X509Certificate2 certificate) =>
    "-----BEGIN CERTIFICATE-----\n" +
    Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) +
    "\n-----END CERTIFICATE-----";

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var list = await _fetcher(cancellationToken);
            var m = BuildMaterials(list);

            // atomowa podmiana referencji wystarcza (właściwości tylko czytają)
            Volatile.Write(ref _materials, m);
        }
        catch
        {
            // Jeżeli mamy stare materiały i nadal mieszczą się w okresie łaski – zostawiamy je.
            var current = Volatile.Read(ref _materials);
            if (current is null || DateTimeOffset.UtcNow > current.ExpiresAt + _staleGrace)
                throw; // nie mamy nic albo już po grace – przekaż wyjątek
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ScheduleNextRefresh()
    {
        var m = Volatile.Read(ref _materials)!;
        var due = m.RefreshAt - DateTimeOffset.UtcNow;
        if (due < TimeSpan.FromSeconds(5)) due = TimeSpan.FromSeconds(5);

        // pojedynczy strzał; po odświeżeniu harmonogramujemy na nowo
        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(async _ =>
        {
            try
            {
                await RefreshAsync(CancellationToken.None);
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
            throw new InvalidOperationException("Brak certyfikatów.");

        var symmetricDto = certs.FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.SymmetricKeyEncryption))
            ?? throw new InvalidOperationException("Brak certyfikatu SymmetricKeyEncryption.");
        var tokenDto = certs.OrderBy(c => c.ValidFrom)
            .FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.KsefTokenEncryption))
            ?? throw new InvalidOperationException("Brak certyfikatu KsefTokenEncryption.");

        var sym = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(symmetricDto.Certificate));
        var tok = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(tokenDto.Certificate));

        var minNotAfterUtc = new[] { sym.NotAfter.ToUniversalTime(), tok.NotAfter.ToUniversalTime() }.Min();
        var expiresAt = new DateTimeOffset(minNotAfterUtc, TimeSpan.Zero);

        // odśwież przed wygaśnięciem lub najpóźniej za maxRevalidateInterval
        var safetyMargin = TimeSpan.FromDays(1);
        var maxRevalidateInterval = TimeSpan.FromHours(24);

        var refreshCandidate = expiresAt - safetyMargin;
        var capByMaxInterval = DateTimeOffset.UtcNow + maxRevalidateInterval;
        var refreshAt = (refreshCandidate < capByMaxInterval) ? refreshCandidate : capByMaxInterval;

        // drobny jitter 0–5 min, by nie wstały wszystkie instancje naraz
        refreshAt -= TimeSpan.FromMinutes(Random.Shared.Next(0, 5));

        return new CertificateMaterials(sym, tok, expiresAt, refreshAt);
    }

    private static InvalidOperationException NotReady() =>
        new("Materiały kryptograficzne nie są jeszcze zainicjalizowane. " +
            "Wywołaj WarmupAsync() na starcie aplikacji lub ForceRefreshAsync().");

    /// <inheritdoc />
    public (string, string) GenerateCsrWithEcdsa(CertificateEnrollmentsInfoResponse certificateInfo)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privateKey = ecdsa.ExportECPrivateKey();

        X500DistinguishedName subject = CreateSubjectDistinguishedName(certificateInfo);

        // Budowanie CSR
        var request = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);

        // Eksport CSR do formatu DER (bajtów)
        byte[] csrDer = request.CreateSigningRequest();
        return (Convert.ToBase64String(csrDer), Convert.ToBase64String(privateKey));
    }

    private static X500DistinguishedName CreateSubjectDistinguishedName(CertificateEnrollmentsInfoResponse certificateInfo)
    {
        var asnWriter = new AsnWriter(AsnEncodingRules.DER);

        void AddRdn(string oid, string value, UniversalTagNumber tag)
        {
            if (string.IsNullOrEmpty(value))
                return;

            using var set = asnWriter.PushSetOf();
            using var seq = asnWriter.PushSequence();
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

    private sealed record CertificateMaterials(
    X509Certificate2 SymmetricKeyCert,
    X509Certificate2 KsefTokenCert,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshAt);
}