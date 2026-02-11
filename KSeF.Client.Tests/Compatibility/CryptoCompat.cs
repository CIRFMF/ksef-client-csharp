#nullable enable
#if NETFRAMEWORK
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;

namespace KSeF.Client.Tests.Compatibility;

/// <summary>
/// Polyfille metod kryptograficznych niedostępnych na .NET Framework 4.8:
/// import (<c>ImportRSAPrivateKey</c>, <c>ImportECPrivateKey</c>) oraz eksport
/// (<c>ExportPkcs8PrivateKeyPemCompat</c> dla ECDsa).
/// </summary>
internal static class CryptoCompat
{
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";

    /// <summary>
    /// Eksportuje klucz prywatny ECDsa w formacie PKCS#8 PrivateKeyInfo zakodowanym jako PEM.
    /// Buduje strukturę: SEC1 ECPrivateKey DER → opakowuje w PKCS#8 → koduje Base64 z nagłówkami PEM.
    /// Polyfill dla <c>ECDsa.ExportPkcs8PrivateKeyPem()</c> dostępnego od .NET 7.
    /// </summary>
    public static string ExportPkcs8PrivateKeyPemCompat(this ECDsa ecdsa)
    {
        ECParameters p = ecdsa.ExportParameters(true);

        string curveOid = GetCurveOid(p.Curve);
        int coordLen = p.D!.Length;

        // 1. Budowanie SEC1 ECPrivateKey DER (RFC 5915)
        // ECPrivateKey ::= SEQUENCE {
        //   version INTEGER { ecPrivkeyVer1(1) },
        //   privateKey OCTET STRING,
        //   parameters [0] ECParameters OPTIONAL,
        //   publicKey  [1] BIT STRING OPTIONAL
        // }
        AsnWriter sec1Writer = new AsnWriter(AsnEncodingRules.DER);
        sec1Writer.PushSequence();
        sec1Writer.WriteInteger(1); // version ecPrivkeyVer1

        sec1Writer.WriteOctetString(p.D);

        // [0] EXPLICIT parametry — OID krzywej
        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        sec1Writer.PushSequence(ctx0);
        sec1Writer.WriteObjectIdentifier(curveOid);
        sec1Writer.PopSequence(ctx0);

        // [1] EXPLICIT klucz publiczny — nieskompresowany punkt EC
        if (p.Q.X != null && p.Q.Y != null)
        {
            byte[] point = new byte[1 + coordLen * 2];
            point[0] = 0x04;
            Buffer.BlockCopy(p.Q.X, 0, point, 1, coordLen);
            Buffer.BlockCopy(p.Q.Y, 0, point, 1 + coordLen, coordLen);

            Asn1Tag ctx1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
            sec1Writer.PushSequence(ctx1);
            sec1Writer.WriteBitString(point);
            sec1Writer.PopSequence(ctx1);
        }

        sec1Writer.PopSequence();
        byte[] sec1Der = sec1Writer.Encode();

        // 2. Opakowywanie SEC1 w PKCS#8 PrivateKeyInfo (RFC 5958)
        // PrivateKeyInfo ::= SEQUENCE {
        //   version INTEGER (0),
        //   privateKeyAlgorithm AlgorithmIdentifier { ecPublicKey, curveOid },
        //   privateKey OCTET STRING { SEC1 DER }
        // }
        AsnWriter pkcs8Writer = new AsnWriter(AsnEncodingRules.DER);
        pkcs8Writer.PushSequence();
        pkcs8Writer.WriteInteger(0); // version
        pkcs8Writer.PushSequence();
        pkcs8Writer.WriteObjectIdentifier(EcPublicKeyOid);
        pkcs8Writer.WriteObjectIdentifier(curveOid);
        pkcs8Writer.PopSequence();
        pkcs8Writer.WriteOctetString(sec1Der);
        pkcs8Writer.PopSequence();
        byte[] pkcs8Der = pkcs8Writer.Encode();

        // 3. Kodowanie PEM
        return "-----BEGIN PRIVATE KEY-----\n" +
               Convert.ToBase64String(pkcs8Der, Base64FormattingOptions.InsertLineBreaks) +
               "\n-----END PRIVATE KEY-----";
    }

    /// <summary>
    /// Wyznacza OID krzywej EC z <see cref="ECCurve"/>.
    /// </summary>
    private static string GetCurveOid(ECCurve curve)
    {
        if (curve.Oid?.Value != null)
            return curve.Oid.Value;

        if (curve.Oid?.FriendlyName != null)
        {
            return curve.Oid.FriendlyName switch
            {
                "nistP256" or "ECDSA_P256" or "ECDH_P256" => "1.2.840.10045.3.1.7",
                "nistP384" or "ECDSA_P384" or "ECDH_P384" => "1.3.132.0.34",
                "nistP521" or "ECDSA_P521" or "ECDH_P521" => "1.3.132.0.35",
                _ => throw new CryptographicException(
                    $"Nie można określić OID dla krzywej EC o nazwie '{curve.Oid.FriendlyName}'.")
            };
        }

        throw new CryptographicException(
            "Nie można określić OID krzywej EC — brak Oid.Value i Oid.FriendlyName.");
    }

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
