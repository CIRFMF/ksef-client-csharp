#nullable enable
#if NETFRAMEWORK
using System.Security.Cryptography;

namespace KSeF.Client.Tests.Core.Compatibility;

/// <summary>
/// Polyfill dla RSA.ImportRSAPrivateKey niedostępnego jako metoda instancyjna na .NET Framework 4.8.
/// Ręcznie parsuje klucz prywatny RSA w formacie PKCS#1 DER i importuje przez <see cref="RSA.ImportParameters"/>.
/// </summary>
internal static class RsaExtensions
{
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
