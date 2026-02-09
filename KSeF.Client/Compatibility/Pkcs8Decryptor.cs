#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Deszyfruje struktury PKCS#8 EncryptedPrivateKeyInfo przy użyciu PBES2 (PBKDF2 + AES-CBC).
/// Zapewnia funkcjonalność równoważną z <c>Pkcs8PrivateKeyInfo.DecryptAndDecode</c> (.NET Core 3.0+).
/// </summary>
/// <remarks>
/// Obsługuje następujące schematy szyfrowania:
/// <list type="bullet">
///   <item><description>PBES2 z PBKDF2 (HMAC-SHA1, HMAC-SHA256, HMAC-SHA384, HMAC-SHA512)</description></item>
///   <item><description>Szyfrowanie AES-128-CBC, AES-192-CBC, AES-256-CBC</description></item>
///   <item><description>Szyfrowanie 3DES-CBC (dla starszych plików kluczy)</description></item>
/// </list>
/// </remarks>
internal static class Pkcs8Decryptor
{
    // OID PBES2
    private const string Pbes2Oid = "1.2.840.113549.1.5.13";

    // OID PBKDF2
    private const string Pbkdf2Oid = "1.2.840.113549.1.5.12";

    // OID algorytmów HMAC
    private const string HmacSha1Oid = "1.2.840.113549.2.7";
    private const string HmacSha256Oid = "1.2.840.113549.2.9";
    private const string HmacSha384Oid = "1.2.840.113549.2.10";
    private const string HmacSha512Oid = "1.2.840.113549.2.11";

    // OID algorytmów szyfrowania
    private const string Aes128CbcOid = "2.16.840.1.101.3.4.1.2";
    private const string Aes192CbcOid = "2.16.840.1.101.3.4.1.22";
    private const string Aes256CbcOid = "2.16.840.1.101.3.4.1.42";
    private const string DesEde3CbcOid = "1.2.840.113549.3.7";

    /// <summary>
    /// Deszyfruje PKCS#8 EncryptedPrivateKeyInfo i zwraca wewnętrzne bajty DER PrivateKeyInfo.
    /// </summary>
    /// <param name="encryptedPkcs8">Zakodowana w DER struktura EncryptedPrivateKeyInfo.</param>
    /// <param name="password">Hasło użyte do deszyfrowania.</param>
    /// <returns>Odszyfrowane bajty DER PrivateKeyInfo.</returns>
    /// <exception cref="CryptographicException">
    /// Nie udało się odszyfrować danych lub schemat szyfrowania nie jest obsługiwany.
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

        // Parsuj AlgorithmIdentifier
        AsnReader algIdSequence = sequence.ReadSequence();
        string encAlgOid = algIdSequence.ReadObjectIdentifier();

        if (encAlgOid != Pbes2Oid)
            throw new CryptographicException(
                $"Nieobsługiwany schemat szyfrowania PKCS#8: '{encAlgOid}'. Obsługiwany jest tylko PBES2 ({Pbes2Oid}).");

        // Parsuj parametry PBES2
        AsnReader pbes2Params = algIdSequence.ReadSequence();
        ParsePbes2Parameters(pbes2Params,
            out byte[] salt, out int iterations, out string prfOid,
            out string encSchemeOid, out byte[] iv, out int keyLength);

        // Odczytaj zaszyfrowane dane
        byte[] encryptedData = sequence.ReadOctetString();

        // Ustal długość klucza ze schematu szyfrowania, jeśli nie podano w KDF
        if (keyLength == 0)
        {
            keyLength = GetKeyLengthForScheme(encSchemeOid);
        }

        // Wyprowadź klucz za pomocą PBKDF2
        byte[] derivedKey = DeriveKey(password, salt, iterations, keyLength, prfOid);

        // Deszyfruj
        byte[] decrypted = DecryptData(derivedKey, iv, encryptedData, encSchemeOid);

        // Zweryfikuj, że wynik jest poprawną sekwencją ASN.1 SEQUENCE (PrivateKeyInfo)
        try
        {
            AsnReader validation = new AsnReader(decrypted, AsnEncodingRules.DER);
            validation.ReadSequence(); // Nie powinno rzucić wyjątku jeśli dane są poprawne
        }
        catch (AsnContentException ex)
        {
            throw new CryptographicException(
                "Odszyfrowane dane PKCS#8 nie są prawidłową strukturą ASN.1. Hasło może być nieprawidłowe.", ex);
        }

        return decrypted;
    }

    /// <summary>
    /// Parsuje strukturę PBES2-params.
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
        // Funkcja wyprowadzania klucza (PBKDF2)
        AsnReader kdfSequence = pbes2Params.ReadSequence();
        string kdfOid = kdfSequence.ReadObjectIdentifier();

        if (kdfOid != Pbkdf2Oid)
            throw new CryptographicException(
                $"Nieobsługiwana funkcja wyprowadzania klucza: '{kdfOid}'. Obsługiwany jest tylko PBKDF2 ({Pbkdf2Oid}).");

        // Parametry PBKDF2
        AsnReader pbkdf2Params = kdfSequence.ReadSequence();
        salt = pbkdf2Params.ReadOctetString();

        // iterations jest BigInteger, ale praktycznie mieści się w int
        System.Numerics.BigInteger iterBig = pbkdf2Params.ReadInteger();
        iterations = (int)iterBig;

        // Opcjonalna długość klucza
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

        // Algorytm PRF (domyślnie HMAC-SHA1 jeśli nie podano)
        prfOid = HmacSha1Oid;
        if (pbkdf2Params.HasData)
        {
            AsnReader prfSequence = pbkdf2Params.ReadSequence();
            prfOid = prfSequence.ReadObjectIdentifier();
        }

        // Schemat szyfrowania
        AsnReader encSequence = pbes2Params.ReadSequence();
        encSchemeOid = encSequence.ReadObjectIdentifier();
        iv = encSequence.ReadOctetString();
    }

    /// <summary>
    /// Wyprowadza klucz za pomocą PBKDF2 z podanym algorytmem PRF.
    /// Na netstandard2.0 <see cref="Rfc2898DeriveBytes"/> natywnie obsługuje tylko HMAC-SHA1
    /// (brak konstruktora z <see cref="HashAlgorithmName"/>). Dla innych PRF
    /// implementujemy PBKDF2 ręcznie z odpowiednim algorytmem HMAC.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength, string prfOid)
    {
        // Na netstandard2.0 Rfc2898DeriveBytes obsługuje tylko HMAC-SHA1.
        // Dla SHA-1 użyj wbudowanej klasy. Dla pozostałych — implementacja ręczna.
        if (prfOid == HmacSha1Oid)
        {
            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            return pbkdf2.GetBytes(keyLength);
        }

        // Ręczna implementacja PBKDF2 dla PRF innych niż SHA1
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        return Pbkdf2Manual(passwordBytes, salt, iterations, keyLength, prfOid);
    }

    /// <summary>
    /// Ręczna implementacja PBKDF2 (RFC 2898) obsługująca dowolne algorytmy HMAC.
    /// Używana na netstandard2.0, gdzie <see cref="Rfc2898DeriveBytes"/> obsługuje tylko HMAC-SHA1.
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
    /// Oblicza pojedynczy blok PBKDF2: U_1 XOR U_2 XOR ... XOR U_c.
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
    /// Tworzy instancję HMAC dla podanego OID algorytmu PRF.
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
    /// Deszyfruje dane przy użyciu podanego schematu szyfrowania.
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
    /// Określa wymaganą długość klucza w bajtach dla danego OID schematu szyfrowania.
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
