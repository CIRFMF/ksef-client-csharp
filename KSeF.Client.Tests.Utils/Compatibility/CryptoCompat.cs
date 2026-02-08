#nullable enable
#if NETFRAMEWORK
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Utils.Compatibility;

/// <summary>
/// Polyfill dla RSA.ImportRSAPrivateKey i X509Certificate2.CopyWithPrivateKey
/// niedostępnych jako metody instancyjne na .NET Framework 4.8.
/// </summary>
internal static class RsaExtensions
{
    /// <summary>
    /// Importuje klucz prywatny RSA w formacie PKCS#1 DER.
    /// </summary>
    public static void ImportRSAPrivateKey(this RSA rsa, byte[] source, out int bytesRead)
    {
        // Na .NET Framework 4.8, RSACryptoServiceProvider nie ma ImportRSAPrivateKey.
        // Użyj RSA.Create() + ImportParameters po ręcznym parsowaniu PKCS#1, albo
        // skorzystaj z CNG: jeśli rsa jest RSACng, możemy użyć ImportParameters.
        // Prostsze podejście: użyj Microsoft.Bcl.Cryptography jeśli dostępne, lub
        // parsuj PKCS#1 ręcznie.
        //
        // Najprostsze: konwertuj PKCS#1 do parametrów RSA ręcznie.
        bytesRead = source.Length;
        RSAParameters parameters = DecodeRSAPrivateKey(source);
        rsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Dekoduje klucz prywatny RSA z formatu PKCS#1 DER (RFC 8017).
    /// </summary>
    private static RSAParameters DecodeRSAPrivateKey(byte[] pkcs1)
    {
        // PKCS#1 RSAPrivateKey ::= SEQUENCE {
        //   version INTEGER, modulus INTEGER, publicExponent INTEGER,
        //   privateExponent INTEGER, prime1 INTEGER, prime2 INTEGER,
        //   exponent1 INTEGER, exponent2 INTEGER, coefficient INTEGER }
        using System.IO.MemoryStream ms = new(pkcs1);
        using System.IO.BinaryReader reader = new(ms);

        // Outer SEQUENCE
        if (reader.ReadByte() != 0x30)
            throw new CryptographicException("Invalid PKCS#1 RSA private key - expected SEQUENCE");
        ReadLength(reader);

        // version
        ReadIntegerRaw(reader); // skip version

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

        // Ensure D, DP, DQ, InverseQ, P, Q are padded to match Modulus length (or half)
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
            throw new CryptographicException("Invalid ASN.1 - expected INTEGER tag");
        int length = ReadLength(reader);
        return reader.ReadBytes(length);
    }

    private static byte[] ReadUnsignedInteger(System.IO.BinaryReader reader)
    {
        byte[] raw = ReadIntegerRaw(reader);
        // Strip leading zero padding (sign byte)
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
