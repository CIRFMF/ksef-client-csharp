#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill extension methods for <see cref="RSA"/> key import/export operations
/// available since .NET 5 / .NET Core 3.0.
/// Uses <see cref="System.Formats.Asn1"/> for ASN.1 DER encoding/decoding.
/// </summary>
internal static class RsaCompat
{
    /// <summary>OID for RSA encryption algorithm (1.2.840.113549.1.1.1).</summary>
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";

    /// <summary>
    /// Imports an RSA key from a PEM-encoded string.
    /// Polyfill for <c>RSA.ImportFromPem(ReadOnlySpan&lt;char&gt;)</c> available since .NET 5.
    /// Supports: RSA PRIVATE KEY (PKCS#1), PRIVATE KEY (PKCS#8), PUBLIC KEY (SPKI), RSA PUBLIC KEY (PKCS#1).
    /// </summary>
    /// <param name="rsa">The RSA instance to import into.</param>
    /// <param name="input">The PEM-encoded key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
    /// <exception cref="CryptographicException">The PEM block is not a recognized RSA key format.</exception>
    public static void ImportFromPem(this RSA rsa, string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        switch (label.ToUpperInvariant())
        {
            case "RSA PRIVATE KEY":
                ImportRsaPrivateKeyCore(rsa, der);
                break;

            case "PRIVATE KEY":
                ImportPkcs8PrivateKey(rsa, der);
                break;

            case "PUBLIC KEY":
                ImportSubjectPublicKeyInfo(rsa, der);
                break;

            case "RSA PUBLIC KEY":
                ImportRsaPublicKeyCore(rsa, der);
                break;

            default:
                throw new CryptographicException(
                    $"Nieobsługiwany typ bloku PEM dla RSA: '{label}'.");
        }
    }

    /// <summary>
    /// Imports an RSA key from an encrypted PEM-encoded string.
    /// Polyfill for <c>RSA.ImportFromEncryptedPem(ReadOnlySpan&lt;char&gt;, ReadOnlySpan&lt;char&gt;)</c> available since .NET 5.
    /// Supports: ENCRYPTED PRIVATE KEY (PKCS#8 encrypted).
    /// </summary>
    /// <param name="rsa">The RSA instance to import into.</param>
    /// <param name="input">The PEM-encoded encrypted key.</param>
    /// <param name="password">The password to decrypt the key.</param>
    public static void ImportFromEncryptedPem(this RSA rsa, string input, string password)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        if (!string.Equals(label, "ENCRYPTED PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException(
                $"Oczekiwano bloku PEM 'ENCRYPTED PRIVATE KEY', otrzymano '{label}'.");

        byte[] decryptedPkcs8 = Pkcs8Decryptor.DecryptPkcs8(der, password);
        ImportPkcs8PrivateKey(rsa, decryptedPkcs8);
    }

    /// <summary>
    /// Exports the RSA private key in PKCS#1 RSAPrivateKey DER format.
    /// Polyfill for <c>RSA.ExportRSAPrivateKey()</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">The RSA instance whose key is to be exported.</param>
    /// <returns>A byte array containing the PKCS#1 DER-encoded private key.</returns>
    public static byte[] ExportRSAPrivateKey(this RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(true);
        return EncodeRsaPrivateKey(parameters);
    }

    /// <summary>
    /// Exports the RSA public key in SubjectPublicKeyInfo (SPKI) DER format.
    /// Polyfill for <c>RSA.ExportSubjectPublicKeyInfo()</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">The RSA instance whose key is to be exported.</param>
    /// <returns>A byte array containing the SPKI DER-encoded public key.</returns>
    public static byte[] ExportSubjectPublicKeyInfo(this RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(false);
        return EncodeSubjectPublicKeyInfo(parameters);
    }

    /// <summary>
    /// Imports a PKCS#1 RSAPrivateKey from a DER-encoded byte array.
    /// Polyfill for <c>RSA.ImportRSAPrivateKey(ReadOnlySpan&lt;byte&gt;, out int)</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">The RSA instance to import into.</param>
    /// <param name="source">The DER-encoded PKCS#1 RSA private key data.</param>
    /// <param name="bytesRead">The number of bytes consumed from <paramref name="source"/>.</param>
    public static void ImportRSAPrivateKey(this RSA rsa, ReadOnlySpan<byte> source, out int bytesRead)
    {
        byte[] sourceArray = source.ToArray();
        ImportRsaPrivateKeyCore(rsa, sourceArray);
        bytesRead = sourceArray.Length;
    }

    #region PKCS#1 RSAPrivateKey ASN.1

    /// <summary>
    /// Decodes a PKCS#1 RSAPrivateKey DER structure and imports it into the RSA instance.
    /// <code>
    /// RSAPrivateKey ::= SEQUENCE {
    ///     version           INTEGER,
    ///     modulus           INTEGER,
    ///     publicExponent    INTEGER,
    ///     privateExponent   INTEGER,
    ///     prime1            INTEGER,
    ///     prime2            INTEGER,
    ///     exponent1         INTEGER,
    ///     exponent2         INTEGER,
    ///     coefficient       INTEGER
    /// }
    /// </code>
    /// </summary>
    private static void ImportRsaPrivateKeyCore(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // version (0)

        RSAParameters parameters = new RSAParameters
        {
            Modulus = ReadUnsignedInteger(sequence),
            Exponent = ReadUnsignedInteger(sequence),
            D = ReadUnsignedInteger(sequence),
            P = ReadUnsignedInteger(sequence),
            Q = ReadUnsignedInteger(sequence),
            DP = ReadUnsignedInteger(sequence),
            DQ = ReadUnsignedInteger(sequence),
            InverseQ = ReadUnsignedInteger(sequence)
        };

        // Ensure private key components are padded to correct length
        int halfModLen = (parameters.Modulus.Length + 1) / 2;
        parameters.D = PadOrTrimLeft(parameters.D, parameters.Modulus.Length);
        parameters.P = PadOrTrimLeft(parameters.P, halfModLen);
        parameters.Q = PadOrTrimLeft(parameters.Q, halfModLen);
        parameters.DP = PadOrTrimLeft(parameters.DP, halfModLen);
        parameters.DQ = PadOrTrimLeft(parameters.DQ, halfModLen);
        parameters.InverseQ = PadOrTrimLeft(parameters.InverseQ, halfModLen);

        rsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Encodes RSA private key parameters as PKCS#1 RSAPrivateKey DER.
    /// </summary>
    private static byte[] EncodeRsaPrivateKey(RSAParameters parameters)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

        writer.PushSequence();
        writer.WriteInteger(0); // version
        WriteUnsignedInteger(writer, parameters.Modulus);
        WriteUnsignedInteger(writer, parameters.Exponent);
        WriteUnsignedInteger(writer, parameters.D);
        WriteUnsignedInteger(writer, parameters.P);
        WriteUnsignedInteger(writer, parameters.Q);
        WriteUnsignedInteger(writer, parameters.DP);
        WriteUnsignedInteger(writer, parameters.DQ);
        WriteUnsignedInteger(writer, parameters.InverseQ);
        writer.PopSequence();

        return writer.Encode();
    }

    #endregion

    #region PKCS#1 RSA Public Key

    /// <summary>
    /// Decodes a PKCS#1 RSAPublicKey and imports it.
    /// <code>
    /// RSAPublicKey ::= SEQUENCE {
    ///     modulus         INTEGER,
    ///     publicExponent  INTEGER
    /// }
    /// </code>
    /// </summary>
    private static void ImportRsaPublicKeyCore(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        RSAParameters parameters = new RSAParameters
        {
            Modulus = ReadUnsignedInteger(sequence),
            Exponent = ReadUnsignedInteger(sequence)
        };

        rsa.ImportParameters(parameters);
    }

    #endregion

    #region SubjectPublicKeyInfo (SPKI)

    /// <summary>
    /// Decodes a SubjectPublicKeyInfo (SPKI) structure containing an RSA public key.
    /// <code>
    /// SubjectPublicKeyInfo ::= SEQUENCE {
    ///     algorithm       AlgorithmIdentifier,
    ///     subjectPublicKey BIT STRING
    /// }
    /// AlgorithmIdentifier ::= SEQUENCE {
    ///     algorithm  OID,
    ///     parameters ANY OPTIONAL  -- NULL for RSA
    /// }
    /// </code>
    /// </summary>
    private static void ImportSubjectPublicKeyInfo(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader spkiSequence = reader.ReadSequence();

        // AlgorithmIdentifier
        AsnReader algId = spkiSequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != RsaEncryptionOid)
            throw new CryptographicException($"Oczekiwano OID RSA ({RsaEncryptionOid}), otrzymano '{oid}'.");

        // Read and discard parameters (NULL for RSA)
        if (algId.HasData)
            algId.ReadNull();

        // SubjectPublicKey BIT STRING → contains PKCS#1 RSAPublicKey
        byte[] publicKeyBits = spkiSequence.ReadBitString(out _);
        ImportRsaPublicKeyCore(rsa, publicKeyBits);
    }

    /// <summary>
    /// Encodes RSA public key parameters as SubjectPublicKeyInfo (SPKI) DER.
    /// </summary>
    private static byte[] EncodeSubjectPublicKeyInfo(RSAParameters parameters)
    {
        // First, encode the inner RSAPublicKey
        AsnWriter innerWriter = new AsnWriter(AsnEncodingRules.DER);
        innerWriter.PushSequence();
        WriteUnsignedInteger(innerWriter, parameters.Modulus);
        WriteUnsignedInteger(innerWriter, parameters.Exponent);
        innerWriter.PopSequence();
        byte[] rsaPublicKey = innerWriter.Encode();

        // Now encode SPKI wrapper
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // AlgorithmIdentifier
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();

        // SubjectPublicKey as BIT STRING
        writer.WriteBitString(rsaPublicKey);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region PKCS#8 PrivateKeyInfo

    /// <summary>
    /// Decodes a PKCS#8 PrivateKeyInfo structure and imports the RSA key.
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///     version                   INTEGER,
    ///     privateKeyAlgorithm       AlgorithmIdentifier,
    ///     privateKey                OCTET STRING  -- contains PKCS#1 RSAPrivateKey
    /// }
    /// </code>
    /// </summary>
    private static void ImportPkcs8PrivateKey(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // version (0)

        // AlgorithmIdentifier
        AsnReader algId = sequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != RsaEncryptionOid)
            throw new CryptographicException($"PKCS#8 zawiera algorytm '{oid}', oczekiwano RSA ({RsaEncryptionOid}).");

        // PrivateKey OCTET STRING → contains PKCS#1 RSAPrivateKey
        byte[] privateKeyOctets = sequence.ReadOctetString();
        ImportRsaPrivateKeyCore(rsa, privateKeyOctets);
    }

    #endregion

    #region ASN.1 Integer Helpers

    /// <summary>
    /// Reads an ASN.1 INTEGER and returns the magnitude bytes (unsigned, no leading zero padding).
    /// </summary>
    private static byte[] ReadUnsignedInteger(AsnReader reader)
    {
        ReadOnlyMemory<byte> value = reader.ReadIntegerBytes();
        byte[] bytes = value.ToArray();

        // Strip leading zero byte used for positive sign in ASN.1
        if (bytes.Length > 1 && bytes[0] == 0)
        {
            byte[] trimmed = new byte[bytes.Length - 1];
            Buffer.BlockCopy(bytes, 1, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        return bytes;
    }

    /// <summary>
    /// Writes an unsigned integer value as ASN.1 INTEGER (adds leading zero if high bit is set).
    /// </summary>
    private static void WriteUnsignedInteger(AsnWriter writer, byte[] value)
    {
        if (value is null || value.Length == 0)
        {
            writer.WriteInteger(0);
            return;
        }

        writer.WriteIntegerUnsigned(new ReadOnlySpan<byte>(value));
    }

    /// <summary>
    /// Pads or trims a byte array to the exact target length.
    /// If shorter, pads with leading zeros. If longer and leading bytes are zero, trims.
    /// </summary>
    private static byte[] PadOrTrimLeft(byte[] data, int targetLength)
    {
        if (data.Length == targetLength)
            return data;

        if (data.Length < targetLength)
        {
            byte[] padded = new byte[targetLength];
            Buffer.BlockCopy(data, 0, padded, targetLength - data.Length, data.Length);
            return padded;
        }

        // Trim leading zeros
        int offset = data.Length - targetLength;
        for (int i = 0; i < offset; i++)
        {
            if (data[i] != 0)
                throw new CryptographicException("Wartość klucza RSA jest zbyt duża dla oczekiwanego rozmiaru.");
        }

        byte[] trimmed = new byte[targetLength];
        Buffer.BlockCopy(data, offset, trimmed, 0, targetLength);
        return trimmed;
    }

    #endregion
}
#endif
