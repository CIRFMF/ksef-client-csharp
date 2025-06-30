using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Sessions;
using KSeFClient;
using KSeFClient.Core.Interfaces;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class CryptographyService : ICryptographyService
{
    private readonly string symetricKeyEncryptionPem;
    private readonly string ksefTokenPem;

    public CryptographyService(IKSeFClient kSeFClient, IRestClient restClient)
    {
        //var certificates = kSeFClient.GetPublicCertificates(default)
        //    .GetAwaiter().GetResult();

        //var symmetricCert = certificates
        //    .FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.SymmetricKeyEncryption));
        //var tokenCert = certificates
        //    .OrderBy(ord => ord.ValidFrom)
        //    .FirstOrDefault(c => c.Usage.Contains(PublicKeyCertificateUsage.KsefTokenEncryption));

        var  pem = restClient.GetPemAsync().GetAwaiter().GetResult();
        symetricKeyEncryptionPem = pem;
        ksefTokenPem = pem;
    }

    /// <inheritdoc />
    public EncryptionData GetEncryptionData()
    {
        var key = GenerateRandom256BitsKey();
        var iv = GenerateRandom16BytesIv();

        var encryptedKey = EncryptWithRSAUsingPublicKey(key, RSAEncryptionPadding.OaepSHA256);
        var encodedEncryptedKey = Convert.ToBase64String(encryptedKey);
        var encryptionInfo = new EncryptionInfo()
        {
            EncryptedSymmetricKey = encodedEncryptedKey,

            InitializationVector = Convert.ToBase64String(iv)

        };
        return new EncryptionData
        {
            CipherKey = key,
            CipherIv = iv,
            EncryptedCipherKey = encodedEncryptedKey,
            EncryptionInfo = encryptionInfo
        };
    }

    /// <inheritdoc />
    public byte[] EncryptBytesWithAES256(byte[] content, byte[] key, byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 16 * 8;
        aes.Key = key;
        aes.IV = iv;

        var encryptor = aes.CreateEncryptor();

        using var input = BinaryData.FromBytes(content).ToStream();
        using var output = new MemoryStream();
        using var cryptoWriter = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
        input.CopyTo(cryptoWriter);
        cryptoWriter.FlushFinalBlock();

        output.Position = 0;
        return BinaryData.FromStream(output).ToArray();
    }

    /// <inheritdoc />
    public (string, string) GenerateCsr(CertificateEnrollmentsInfoResponse certificateInfo)
    {
        using var rsa = RSA.Create(2048);
        var asnWriter = new AsnWriter(AsnEncodingRules.DER);

        var publicKey = rsa.ExportRSAPublicKey();
        var privateKey = rsa.ExportRSAPrivateKey();

        void AddRdn(string oid, string value, UniversalTagNumber universalTagNumber)
        {
            if (string.IsNullOrEmpty(value))
                return;

            using var set = asnWriter.PushSetOf();
            using var sequence = asnWriter.PushSequence();

            asnWriter.WriteObjectIdentifier(oid);
            if (universalTagNumber == UniversalTagNumber.BitString)
                asnWriter.WriteBitString(Encoding.UTF8.GetBytes(value));
            else
                asnWriter.WriteCharacterString(universalTagNumber, value);

        }

        using (var sequence = asnWriter.PushSequence())
        {
            AddRdn("2.5.4.3", certificateInfo.CommonName, UniversalTagNumber.UTF8String); //CN
            AddRdn("2.5.4.4", certificateInfo.Surname, UniversalTagNumber.UTF8String); //SN
            AddRdn("2.5.4.10", certificateInfo.OrganizationName, UniversalTagNumber.UTF8String); //ON
            AddRdn("2.5.4.97", certificateInfo.OrganizationIdentifier, UniversalTagNumber.UTF8String); //OID
            AddRdn("2.5.4.6", certificateInfo.CountryName, UniversalTagNumber.PrintableString); //C
            AddRdn("2.5.4.5", certificateInfo.SerialNumber, UniversalTagNumber.PrintableString); //SERIALNUMBER
            AddRdn("2.5.4.45", certificateInfo.UniqueIdentifier, UniversalTagNumber.BitString); //UID

            foreach (var givenName in certificateInfo.GivenNames ?? Enumerable.Empty<string>())
                AddRdn("2.5.4.42", givenName, UniversalTagNumber.UTF8String); //GN
        }

        var subject = new X500DistinguishedName(asnWriter.Encode());

        // Budowanie CSR
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Eksport CSR do formatu DER (bajtów)
        var csrDer = request.CreateSigningRequest();
        return (Convert.ToBase64String(csrDer), Convert.ToBase64String(privateKey));
    }

    /// <inheritdoc />
    public FileMetadata GetMetaData(byte[] file)
    {

        var base64Hash = "";
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(file);
            base64Hash = Convert.ToBase64String(hash);
        }

        var fileSize = file.Length;

        return new FileMetadata
        {
            FileSize = fileSize,
            HashSHA = base64Hash
        };
    }

    /// <inheritdoc />
    public byte[] EncryptWithRSAUsingPublicKey(byte[] content, RSAEncryptionPadding padding)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(symetricKeyEncryptionPem);
        return rsa.Encrypt(content, padding);
    }

    /// <inheritdoc />
    public byte[] EncryptKsefTokenWithRSAUsingPublicKey(byte[] content)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(ksefTokenPem);
        return rsa.Encrypt(content, RSAEncryptionPadding.OaepSHA256);
    }

    private byte[] GenerateRandom256BitsKey()
    {
        var key = new byte[256 / 8];
        var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        return key;
    }

    private byte[] GenerateRandom16BytesIv()
    {
        var iv = new byte[16];
        var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);

        return iv;
    }

}