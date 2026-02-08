#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Decrypts PKCS#8 EncryptedPrivateKeyInfo structures using PBES2 (PBKDF2 + AES-CBC).
/// Provides functionality equivalent to <c>Pkcs8PrivateKeyInfo.DecryptAndDecode</c> (.NET Core 3.0+).
/// </summary>
/// <remarks>
/// Supports the following encryption schemes:
/// <list type="bullet">
///   <item><description>PBES2 with PBKDF2 (HMAC-SHA1, HMAC-SHA256, HMAC-SHA384, HMAC-SHA512)</description></item>
///   <item><description>AES-128-CBC, AES-192-CBC, AES-256-CBC encryption</description></item>
///   <item><description>3DES-CBC encryption (for older key files)</description></item>
/// </list>
/// </remarks>
internal static class Pkcs8Decryptor
{
    // PBES2 OID
    private const string Pbes2Oid = "1.2.840.113549.1.5.13";

    // PBKDF2 OID
    private const string Pbkdf2Oid = "1.2.840.113549.1.5.12";

    // HMAC algorithm OIDs
    private const string HmacSha1Oid = "1.2.840.113549.2.7";
    private const string HmacSha256Oid = "1.2.840.113549.2.9";
    private const string HmacSha384Oid = "1.2.840.113549.2.10";
    private const string HmacSha512Oid = "1.2.840.113549.2.11";

    // Encryption algorithm OIDs
    private const string Aes128CbcOid = "2.16.840.1.101.3.4.1.2";
    private const string Aes192CbcOid = "2.16.840.1.101.3.4.1.22";
    private const string Aes256CbcOid = "2.16.840.1.101.3.4.1.42";
    private const string DesEde3CbcOid = "1.2.840.113549.3.7";

    /// <summary>
    /// Decrypts a PKCS#8 EncryptedPrivateKeyInfo and returns the inner PrivateKeyInfo DER bytes.
    /// </summary>
    /// <param name="encryptedPkcs8">The DER-encoded EncryptedPrivateKeyInfo.</param>
    /// <param name="password">The password used to decrypt.</param>
    /// <returns>The decrypted PrivateKeyInfo DER bytes.</returns>
    /// <exception cref="CryptographicException">
    /// The encrypted data could not be decrypted, or the encryption scheme is not supported.
    /// </exception>
    /// <remarks>
    /// <code>
    /// EncryptedPrivateKeyInfo ::= SEQUENCE {
    ///     encryptionAlgorithm  AlgorithmIdentifier,
    ///     encryptedData        OCTET STRING
    /// }
    /// </code>
    /// </remarks>
    public static byte[] DecryptPkcs8(byte[] encryptedPkcs8, string password)
    {
        AsnReader reader = new AsnReader(encryptedPkcs8, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        // Parse AlgorithmIdentifier
        AsnReader algIdSequence = sequence.ReadSequence();
        string encAlgOid = algIdSequence.ReadObjectIdentifier();

        if (encAlgOid != Pbes2Oid)
            throw new CryptographicException(
                $"Nieobsługiwany schemat szyfrowania PKCS#8: '{encAlgOid}'. Obsługiwany jest tylko PBES2 ({Pbes2Oid}).");

        // Parse PBES2 parameters
        AsnReader pbes2Params = algIdSequence.ReadSequence();
        ParsePbes2Parameters(pbes2Params,
            out byte[] salt, out int iterations, out string prfOid,
            out string encSchemeOid, out byte[] iv, out int keyLength);

        // Read encrypted data
        byte[] encryptedData = sequence.ReadOctetString();

        // Determine key length from encryption scheme if not specified in KDF
        if (keyLength == 0)
        {
            keyLength = GetKeyLengthForScheme(encSchemeOid);
        }

        // Derive key using PBKDF2
        byte[] derivedKey = DeriveKey(password, salt, iterations, keyLength, prfOid);

        // Decrypt
        byte[] decrypted = DecryptData(derivedKey, iv, encryptedData, encSchemeOid);

        // Validate that the result is a valid ASN.1 SEQUENCE (PrivateKeyInfo)
        try
        {
            AsnReader validation = new AsnReader(decrypted, AsnEncodingRules.DER);
            validation.ReadSequence(); // Should not throw if valid
        }
        catch (AsnContentException ex)
        {
            throw new CryptographicException(
                "Odszyfrowane dane PKCS#8 nie są prawidłową strukturą ASN.1. Hasło może być nieprawidłowe.", ex);
        }

        return decrypted;
    }

    /// <summary>
    /// Parses PBES2-params structure.
    /// <code>
    /// PBES2-params ::= SEQUENCE {
    ///     keyDerivationFunc AlgorithmIdentifier {{ PBES2-KDFs }},
    ///     encryptionScheme  AlgorithmIdentifier {{ PBES2-Encs }}
    /// }
    /// </code>
    /// </summary>
    private static void ParsePbes2Parameters(
        AsnReader pbes2Params,
        out byte[] salt, out int iterations, out string prfOid,
        out string encSchemeOid, out byte[] iv, out int keyLength)
    {
        // Key Derivation Function (PBKDF2)
        AsnReader kdfSequence = pbes2Params.ReadSequence();
        string kdfOid = kdfSequence.ReadObjectIdentifier();

        if (kdfOid != Pbkdf2Oid)
            throw new CryptographicException(
                $"Nieobsługiwana funkcja wyprowadzania klucza: '{kdfOid}'. Obsługiwany jest tylko PBKDF2 ({Pbkdf2Oid}).");

        // PBKDF2-params
        AsnReader pbkdf2Params = kdfSequence.ReadSequence();
        salt = pbkdf2Params.ReadOctetString();

        // iterations is BigInteger but practically fits in int
        System.Numerics.BigInteger iterBig = pbkdf2Params.ReadInteger();
        iterations = (int)iterBig;

        // Optional keyLength
        keyLength = 0;
        if (pbkdf2Params.HasData)
        {
            Asn1Tag nextTag = pbkdf2Params.PeekTag();
            if (nextTag.TagValue == (int)UniversalTagNumber.Integer && nextTag.TagClass == TagClass.Universal)
            {
                System.Numerics.BigInteger keyLenBig = pbkdf2Params.ReadInteger();
                keyLength = (int)keyLenBig;
            }
        }

        // PRF algorithm (default HMAC-SHA1 if not present)
        prfOid = HmacSha1Oid;
        if (pbkdf2Params.HasData)
        {
            AsnReader prfSequence = pbkdf2Params.ReadSequence();
            prfOid = prfSequence.ReadObjectIdentifier();
        }

        // Encryption Scheme
        AsnReader encSequence = pbes2Params.ReadSequence();
        encSchemeOid = encSequence.ReadObjectIdentifier();
        iv = encSequence.ReadOctetString();
    }

    /// <summary>
    /// Derives a key using PBKDF2 with the specified PRF algorithm.
    /// On netstandard2.0, <see cref="Rfc2898DeriveBytes"/> only supports HMAC-SHA1
    /// natively (no <see cref="HashAlgorithmName"/> constructor). For other PRFs,
    /// we implement PBKDF2 manually using the appropriate HMAC algorithm.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength, string prfOid)
    {
        // On netstandard2.0, Rfc2898DeriveBytes only has HMAC-SHA1 support.
        // For SHA-1, use the built-in class. For others, implement manually.
        if (prfOid == HmacSha1Oid)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            return pbkdf2.GetBytes(keyLength);
        }

        // Manual PBKDF2 implementation for non-SHA1 PRFs
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        return Pbkdf2Manual(passwordBytes, salt, iterations, keyLength, prfOid);
    }

    /// <summary>
    /// Manual PBKDF2 implementation (RFC 2898) supporting arbitrary HMAC algorithms.
    /// Used on netstandard2.0 where <see cref="Rfc2898DeriveBytes"/> only supports HMAC-SHA1.
    /// </summary>
    private static byte[] Pbkdf2Manual(byte[] password, byte[] salt, int iterations, int keyLength, string prfOid)
    {
        using HMAC hmac = CreateHmac(prfOid, password);
        int hashLength = hmac.HashSize / 8;
        int blocksNeeded = (keyLength + hashLength - 1) / hashLength;

        byte[] derivedKey = new byte[keyLength];
        int offset = 0;

        for (int blockIndex = 1; blockIndex <= blocksNeeded; blockIndex++)
        {
            byte[] block = Pbkdf2Block(hmac, salt, iterations, blockIndex);
            int bytesToCopy = Math.Min(hashLength, keyLength - offset);
            Buffer.BlockCopy(block, 0, derivedKey, offset, bytesToCopy);
            offset += bytesToCopy;
        }

        return derivedKey;
    }

    /// <summary>
    /// Computes a single PBKDF2 block: U_1 XOR U_2 XOR ... XOR U_c.
    /// </summary>
    private static byte[] Pbkdf2Block(HMAC hmac, byte[] salt, int iterations, int blockIndex)
    {
        // U_1 = PRF(Password, Salt || INT_32_BE(i))
        byte[] input = new byte[salt.Length + 4];
        Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
        input[salt.Length + 0] = (byte)(blockIndex >> 24);
        input[salt.Length + 1] = (byte)(blockIndex >> 16);
        input[salt.Length + 2] = (byte)(blockIndex >> 8);
        input[salt.Length + 3] = (byte)(blockIndex);

        byte[] u = hmac.ComputeHash(input);
        byte[] result = (byte[])u.Clone();

        // U_2 ... U_c
        for (int i = 1; i < iterations; i++)
        {
            u = hmac.ComputeHash(u);
            for (int j = 0; j < result.Length; j++)
            {
                result[j] ^= u[j];
            }
        }

        return result;
    }

    /// <summary>
    /// Creates an HMAC instance for the specified PRF OID.
    /// </summary>
    private static HMAC CreateHmac(string prfOid, byte[] key)
    {
        return prfOid switch
        {
            HmacSha1Oid => new HMACSHA1(key),
            HmacSha256Oid => new HMACSHA256(key),
            HmacSha384Oid => new HMACSHA384(key),
            HmacSha512Oid => new HMACSHA512(key),
            _ => throw new CryptographicException(
                $"Nieobsługiwany algorytm PRF: '{prfOid}'.")
        };
    }

    /// <summary>
    /// Decrypts data using the specified encryption scheme.
    /// </summary>
    private static byte[] DecryptData(byte[] key, byte[] iv, byte[] encryptedData, string encSchemeOid)
    {
        SymmetricAlgorithm algorithm;

        switch (encSchemeOid)
        {
            case Aes128CbcOid:
            case Aes192CbcOid:
            case Aes256CbcOid:
                Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = key.Length * 8;
                algorithm = aes;
                break;

            case DesEde3CbcOid:
                TripleDES tdes = TripleDES.Create();
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.PKCS7;
                algorithm = tdes;
                break;

            default:
                throw new CryptographicException(
                    $"Nieobsługiwany schemat szyfrowania: '{encSchemeOid}'.");
        }

        using (algorithm)
        {
            algorithm.Key = key;
            algorithm.IV = iv;

            using ICryptoTransform decryptor = algorithm.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
    }

    /// <summary>
    /// Determines the required key length in bytes for a given encryption scheme OID.
    /// </summary>
    private static int GetKeyLengthForScheme(string encSchemeOid)
    {
        return encSchemeOid switch
        {
            Aes128CbcOid => 16,
            Aes192CbcOid => 24,
            Aes256CbcOid => 32,
            DesEde3CbcOid => 24,
            _ => throw new CryptographicException(
                $"Nie można określić długości klucza dla schematu: '{encSchemeOid}'.")
        };
    }
}
#endif
