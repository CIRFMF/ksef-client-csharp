#nullable enable
#if NETFRAMEWORK
using System.IO;
using System.Security.Cryptography;

namespace KSeF.Client.Tests.Compatibility;

/// <summary>
/// Polyfill dla RSA.ImportRSAPrivateKey i ECDsa.ImportECPrivateKey
/// niedostepnych jako metody instancyjne na .NET Framework 4.8.
/// </summary>
internal static class CryptoCompat
{
    /// <summary>
    /// Importuje klucz prywatny RSA w formacie PKCS#1 DER.
    /// Polyfill dla RSA.ImportRSAPrivateKey dostepnego od .NET Core 3.0.
    /// </summary>
    public static void ImportRSAPrivateKey(this RSA rsa, byte[] source, out int bytesRead)
    {
        bytesRead = source.Length;
        RSAParameters parameters = DecodeRSAPrivateKey(source);
        rsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Importuje klucz prywatny ECDSA w formacie SEC1 (ECPrivateKey) DER.
    /// Polyfill dla ECDsa.ImportECPrivateKey dostepnego od .NET Core 3.0.
    /// </summary>
    public static void ImportECPrivateKey(this ECDsa ecdsa, byte[] source, out int bytesRead)
    {
        bytesRead = source.Length;
        ECParameters parameters = DecodeECPrivateKey(source);
        ecdsa.ImportParameters(parameters);
    }

    #region RSA PKCS#1 decoding

    private static RSAParameters DecodeRSAPrivateKey(byte[] pkcs1)
    {
        using MemoryStream ms = new(pkcs1);
        using BinaryReader reader = new(ms);

        if (reader.ReadByte() != 0x30)
            throw new CryptographicException("Invalid PKCS#1 RSA private key - expected SEQUENCE");
        ReadLength(reader);

        ReadIntegerRaw(reader); // pomiń wersję

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

    #endregion

    #region ECDsa SEC1 decoding

    /// <summary>
    /// Mapowania OID dla nazwanych krzywych.
    /// </summary>
    private static readonly Dictionary<string, ECCurve> OidToCurve = new()
    {
        ["1.2.840.10045.3.1.7"] = ECCurve.NamedCurves.nistP256,
        ["1.3.132.0.34"] = ECCurve.NamedCurves.nistP384,
        ["1.3.132.0.35"] = ECCurve.NamedCurves.nistP521,
    };

    private static ECParameters DecodeECPrivateKey(byte[] der)
    {
        using MemoryStream ms = new(der);
        using BinaryReader reader = new(ms);

        // Główny SEQUENCE
        if (reader.ReadByte() != 0x30)
            throw new CryptographicException("Invalid SEC1 ECPrivateKey - expected SEQUENCE");
        ReadLength(reader);

        // wersja INTEGER (1)
        ReadIntegerRaw(reader);

        // klucz prywatny OCTET STRING
        byte[] privateKeyBytes = ReadOctetString(reader);

        // Opcjonalne parametry [0] — OID krzywej
        ECCurve curve = default;
        bool hasCurve = false;

        if (ms.Position < ms.Length)
        {
            byte tag = reader.ReadByte();
            if ((tag & 0xE0) == 0xA0) // tag kontekstowy [0]
            {
                int len = ReadLength(reader);
                // Odczytaj OID
                if (reader.ReadByte() == 0x06) // tag OID
                {
                    int oidLen = ReadLength(reader);
                    byte[] oidBytes = reader.ReadBytes(oidLen);
                    string oid = DecodeOid(oidBytes);
                    if (OidToCurve.TryGetValue(oid, out ECCurve namedCurve))
                    {
                        curve = namedCurve;
                        hasCurve = true;
                    }
                }
            }
        }

        if (!hasCurve)
        {
            // Domyślnie P-256 jeśli nie znaleziono OID krzywej
            curve = ECCurve.NamedCurves.nistP256;
        }

        // Określ rozmiar klucza z krzywej lub bajtów klucza prywatnego
        int keySize = GetKeySizeForCurve(curve);
        byte[] d = PadLeft(privateKeyBytes, keySize);

        // Spróbuj odczytać opcjonalny klucz publiczny [1]
        byte[]? qx = null;
        byte[]? qy = null;

        if (ms.Position < ms.Length)
        {
            byte tag = reader.ReadByte();
            if ((tag & 0xE0) == 0xA0 && (tag & 0x1F) == 1) // tag kontekstowy [1]
            {
                int len = ReadLength(reader);
                // BIT STRING — klucz publiczny
                if (reader.ReadByte() == 0x03)
                {
                    int bitLen = ReadLength(reader);
                    reader.ReadByte(); // nieużywane bity (powinno być 0)
                    byte[] pubKeyBytes = reader.ReadBytes(bitLen - 1);

                    if (pubKeyBytes.Length > 0 && pubKeyBytes[0] == 0x04) // nieskompresowany punkt
                    {
                        int coordLen = (pubKeyBytes.Length - 1) / 2;
                        qx = new byte[coordLen];
                        qy = new byte[coordLen];
                        Buffer.BlockCopy(pubKeyBytes, 1, qx, 0, coordLen);
                        Buffer.BlockCopy(pubKeyBytes, 1 + coordLen, qy, 0, coordLen);
                    }
                }
            }
        }

        ECParameters ecParams = new()
        {
            Curve = curve,
            D = d,
        };

        if (qx != null && qy != null)
        {
            ecParams.Q = new ECPoint { X = qx, Y = qy };
        }
        else
        {
            // Wygeneruj klucz publiczny z prywatnego przez import i re-eksport
            using ECDsa tempEcdsa = ECDsa.Create();
            // Najpierw ustaw fikcyjne Q, żeby ImportParameters nie zwrócił błędu
            ecParams.Q = new ECPoint
            {
                X = new byte[keySize],
                Y = new byte[keySize]
            };

            // Obejście: utwórz z parametrów z kluczem prywatnym, potem wyeksportuj żeby uzyskać Q
            try
            {
                tempEcdsa.ImportParameters(ecParams);
                ECParameters exportedParams = tempEcdsa.ExportParameters(true);
                ecParams.Q = exportedParams.Q;
            }
            catch
            {
                // Jeśli import z fikcyjnym Q się nie powiedzie, zostaw Q jak jest
            }
        }

        return ecParams;
    }

    private static int GetKeySizeForCurve(ECCurve curve)
    {
        if (curve.Oid?.Value == "1.2.840.10045.3.1.7" ||
            curve.Oid?.FriendlyName == "nistP256" ||
            curve.Oid?.FriendlyName == "ECDSA_P256")
            return 32;
        if (curve.Oid?.Value == "1.3.132.0.34" ||
            curve.Oid?.FriendlyName == "nistP384" ||
            curve.Oid?.FriendlyName == "ECDSA_P384")
            return 48;
        if (curve.Oid?.Value == "1.3.132.0.35" ||
            curve.Oid?.FriendlyName == "nistP521" ||
            curve.Oid?.FriendlyName == "ECDSA_P521")
            return 66;
        return 32; // default to P-256
    }

    private static string DecodeOid(byte[] oidBytes)
    {
        if (oidBytes.Length == 0) return string.Empty;

        List<int> components = new();
        // Pierwszy bajt koduje dwa pierwsze komponenty
        components.Add(oidBytes[0] / 40);
        components.Add(oidBytes[0] % 40);

        int value = 0;
        for (int i = 1; i < oidBytes.Length; i++)
        {
            value = (value << 7) | (oidBytes[i] & 0x7F);
            if ((oidBytes[i] & 0x80) == 0)
            {
                components.Add(value);
                value = 0;
            }
        }

        return string.Join(".", components);
    }

    private static byte[] ReadOctetString(BinaryReader reader)
    {
        if (reader.ReadByte() != 0x04) // tag OCTET STRING
            throw new CryptographicException("Expected OCTET STRING tag");
        int length = ReadLength(reader);
        return reader.ReadBytes(length);
    }

    #endregion

    #region ASN.1 helpers

    private static byte[] PadLeft(byte[] data, int targetLength)
    {
        if (data.Length >= targetLength) return data;
        byte[] padded = new byte[targetLength];
        Buffer.BlockCopy(data, 0, padded, targetLength - data.Length, data.Length);
        return padded;
    }

    private static int ReadLength(BinaryReader reader)
    {
        byte b = reader.ReadByte();
        if (b < 0x80) return b;
        int numBytes = b & 0x7F;
        int length = 0;
        for (int i = 0; i < numBytes; i++)
            length = (length << 8) | reader.ReadByte();
        return length;
    }

    private static byte[] ReadIntegerRaw(BinaryReader reader)
    {
        if (reader.ReadByte() != 0x02)
            throw new CryptographicException("Invalid ASN.1 - expected INTEGER tag");
        int length = ReadLength(reader);
        return reader.ReadBytes(length);
    }

    private static byte[] ReadUnsignedInteger(BinaryReader reader)
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

    #endregion
}

#endif
