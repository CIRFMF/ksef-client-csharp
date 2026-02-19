#nullable enable
#if NETFRAMEWORK
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Tests.Core.Compatibility;

/// <summary>
/// Polyfille metod <see cref="RSA"/> niedostępnych na .NET Framework 4.8:
/// import (<c>ImportRSAPrivateKey</c>) oraz eksport (<c>ExportRSAPrivateKey</c>,
/// <c>ExportSubjectPublicKeyInfo</c>, <c>ExportPkcs8PrivateKey</c>).
/// </summary>
internal static class RsaExtensions
{
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";

    /// <summary>
    /// Eksportuje klucz prywatny RSA w formacie PKCS#1 DER (RFC 8017 A.1.2).
    /// Polyfill dla <c>RSA.ExportRSAPrivateKey()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    public static byte[] ExportRSAPrivateKey(this RSA rsa)
    {
        RSAParameters p = rsa.ExportParameters(true);
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(0); // version
        writer.WriteIntegerUnsigned(p.Modulus);
        writer.WriteIntegerUnsigned(p.Exponent);
        writer.WriteIntegerUnsigned(p.D);
        writer.WriteIntegerUnsigned(p.P);
        writer.WriteIntegerUnsigned(p.Q);
        writer.WriteIntegerUnsigned(p.DP);
        writer.WriteIntegerUnsigned(p.DQ);
        writer.WriteIntegerUnsigned(p.InverseQ);
        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Eksportuje klucz publiczny RSA w formacie SubjectPublicKeyInfo DER (RFC 5280).
    /// Polyfill dla <c>RSA.ExportSubjectPublicKeyInfo()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    public static byte[] ExportSubjectPublicKeyInfo(this RSA rsa)
    {
        RSAParameters p = rsa.ExportParameters(false);
        // Kodowanie PKCS#1 RSAPublicKey DER
        AsnWriter pubKeyWriter = new AsnWriter(AsnEncodingRules.DER);
        pubKeyWriter.PushSequence();
        pubKeyWriter.WriteIntegerUnsigned(p.Modulus);
        pubKeyWriter.WriteIntegerUnsigned(p.Exponent);
        pubKeyWriter.PopSequence();
        byte[] pubKeyDer = pubKeyWriter.Encode();

        // Opakowywanie w SubjectPublicKeyInfo
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();
        writer.WriteBitString(pubKeyDer);
        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Eksportuje klucz prywatny RSA w formacie PKCS#8 PrivateKeyInfo DER (RFC 5958).
    /// Opakowuje PKCS#1 RSAPrivateKey w strukturę PKCS#8:
    /// <c>SEQUENCE { INTEGER 0, AlgorithmIdentifier { rsaEncryption, NULL }, OCTET STRING { PKCS#1 DER } }</c>.
    /// Polyfill dla <c>RSA.ExportPkcs8PrivateKey()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    public static byte[] ExportPkcs8PrivateKey(this RSA rsa)
    {
        byte[] pkcs1Der = rsa.ExportRSAPrivateKey();
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(0); // version
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();
        writer.WriteOctetString(pkcs1Der);
        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Importuje klucz prywatny RSA w formacie PKCS#1 DER.
    /// Polyfill dla RSA.ImportRSAPrivateKey dostępnego od .NET Core 3.0.
    /// </summary>
    public static void ImportRSAPrivateKey(this RSA rsa, byte[] source, out int bytesRead)
    {
        bytesRead = source.Length;
        RSAParameters parameters = DecodeRSAPrivateKey(source);
        rsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Dekoduje klucz prywatny RSA z formatu PKCS#1 DER (RFC 8017).
    /// </summary>
    private static RSAParameters DecodeRSAPrivateKey(byte[] pkcs1)
    {
        using System.IO.MemoryStream ms = new(pkcs1);
        using System.IO.BinaryReader reader = new(ms);
        if (reader.ReadByte() != 0x30)
            throw new CryptographicException("Invalid PKCS#1 RSA private key");
        ReadLength(reader);
        ReadIntegerRaw(reader);
        RSAParameters p = new()
        {
            Modulus = ReadUnsignedInteger(reader),
            Exponent = ReadUnsignedInteger(reader),
            D = ReadUnsignedInteger(reader),
            P = ReadUnsignedInteger(reader),
            Q = ReadUnsignedInteger(reader),
            DP = ReadUnsignedInteger(reader),
            DQ = ReadUnsignedInteger(reader),
            InverseQ = ReadUnsignedInteger(reader)
        };
        int modulusLen = p.Modulus!.Length;
        int halfLen = (modulusLen + 1) / 2;
        p.D = PadLeft(p.D!, modulusLen);
        p.P = PadLeft(p.P!, halfLen);
        p.Q = PadLeft(p.Q!, halfLen);
        p.DP = PadLeft(p.DP!, halfLen);
        p.DQ = PadLeft(p.DQ!, halfLen);
        p.InverseQ = PadLeft(p.InverseQ!, halfLen);
        return p;
    }

    private static byte[] PadLeft(byte[] data, int targetLength)
    {
        if (data.Length >= targetLength) return data;
        byte[] padded = new byte[targetLength];
        Buffer.BlockCopy(data, 0, padded, targetLength - data.Length, data.Length);
        return padded;
    }

    private static int ReadLength(System.IO.BinaryReader reader)
    {
        byte b = reader.ReadByte();
        if (b < 0x80) return b;
        int numBytes = b & 0x7F;
        int length = 0;
        for (int i = 0; i < numBytes; i++)
            length = (length << 8) | reader.ReadByte();
        return length;
    }

    private static byte[] ReadIntegerRaw(System.IO.BinaryReader reader)
    {
        if (reader.ReadByte() != 0x02)
            throw new CryptographicException("Invalid ASN.1 - expected INTEGER");
        int length = ReadLength(reader);
        return reader.ReadBytes(length);
    }

    private static byte[] ReadUnsignedInteger(System.IO.BinaryReader reader)
    {
        byte[] raw = ReadIntegerRaw(reader);
        if (raw.Length > 1 && raw[0] == 0x00)
        {
            byte[] trimmed = new byte[raw.Length - 1];
            Buffer.BlockCopy(raw, 1, trimmed, 0, trimmed.Length);
            return trimmed;
        }
        return raw;
    }
}
#endif
