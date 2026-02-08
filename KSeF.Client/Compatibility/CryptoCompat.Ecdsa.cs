#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill extension methods for <see cref="ECDsa"/> key import/export operations
/// available since .NET 5 / .NET Core 3.0.
/// Uses <see cref="System.Formats.Asn1"/> for ASN.1 DER encoding/decoding.
/// </summary>
internal static class EcdsaCompat
{
    /// <summary>OID for EC public key algorithm (1.2.840.10045.2.1).</summary>
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";

    /// <summary>OID for NIST P-256 curve (1.2.840.10045.3.1.7).</summary>
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    /// <summary>OID for NIST P-384 curve (1.3.132.0.34).</summary>
    private const string NistP384Oid = "1.3.132.0.34";

    /// <summary>OID for NIST P-521 curve (1.3.132.0.35).</summary>
    private const string NistP521Oid = "1.3.132.0.35";

    /// <summary>
    /// Imports an ECDsa key from a PEM-encoded string.
    /// Polyfill for <c>ECDsa.ImportFromPem(ReadOnlySpan&lt;char&gt;)</c> available since .NET 5.
    /// Supports: EC PRIVATE KEY (SEC1), PRIVATE KEY (PKCS#8), PUBLIC KEY (SPKI).
    /// </summary>
    /// <param name="ecdsa">The ECDsa instance to import into.</param>
    /// <param name="input">The PEM-encoded key.</param>
    public static void ImportFromPem(this ECDsa ecdsa, string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        switch (label.ToUpperInvariant())
        {
            case "EC PRIVATE KEY":
                ImportEcPrivateKeyCore(ecdsa, der);
                break;

            case "PRIVATE KEY":
                ImportPkcs8PrivateKey(ecdsa, der);
                break;

            case "PUBLIC KEY":
                ImportSubjectPublicKeyInfo(ecdsa, der);
                break;

            default:
                throw new CryptographicException(
                    $"Nieobsługiwany typ bloku PEM dla ECDsa: '{label}'.");
        }
    }

    /// <summary>
    /// Imports an ECDsa key from an encrypted PEM-encoded string.
    /// Polyfill for <c>ECDsa.ImportFromEncryptedPem(ReadOnlySpan&lt;char&gt;, ReadOnlySpan&lt;char&gt;)</c> available since .NET 5.
    /// </summary>
    /// <param name="ecdsa">The ECDsa instance to import into.</param>
    /// <param name="input">The PEM-encoded encrypted key.</param>
    /// <param name="password">The password to decrypt the key.</param>
    public static void ImportFromEncryptedPem(this ECDsa ecdsa, string input, string password)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        if (!string.Equals(label, "ENCRYPTED PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException(
                $"Oczekiwano bloku PEM 'ENCRYPTED PRIVATE KEY', otrzymano '{label}'.");

        byte[] decryptedPkcs8 = Pkcs8Decryptor.DecryptPkcs8(der, password);
        ImportPkcs8PrivateKey(ecdsa, decryptedPkcs8);
    }

    /// <summary>
    /// Exports the EC private key in SEC1 ECPrivateKey DER format.
    /// Polyfill for <c>ECDsa.ExportECPrivateKey()</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">The ECDsa instance whose key is to be exported.</param>
    /// <returns>A byte array containing the SEC1 DER-encoded private key.</returns>
    public static byte[] ExportECPrivateKey(this ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(true);
        return EncodeEcPrivateKey(parameters);
    }

    /// <summary>
    /// Exports the ECDsa public key in SubjectPublicKeyInfo (SPKI) DER format.
    /// Polyfill for <c>ECDsa.ExportSubjectPublicKeyInfo()</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">The ECDsa instance whose key is to be exported.</param>
    /// <returns>A byte array containing the SPKI DER-encoded public key.</returns>
    public static byte[] ExportSubjectPublicKeyInfo(this ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(false);
        return EncodeSubjectPublicKeyInfo(parameters);
    }

    /// <summary>
    /// Imports a PKCS#8 PrivateKeyInfo from a DER-encoded byte array.
    /// Polyfill for <c>ECDsa.ImportPkcs8PrivateKey(ReadOnlySpan&lt;byte&gt;, out int)</c> available since .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">The ECDsa instance to import into.</param>
    /// <param name="source">The DER-encoded PKCS#8 private key data.</param>
    /// <param name="bytesRead">The number of bytes consumed from <paramref name="source"/>.</param>
    public static void ImportPkcs8PrivateKey(this ECDsa ecdsa, ReadOnlySpan<byte> source, out int bytesRead)
    {
        byte[] sourceArray = source.ToArray();
        ImportPkcs8PrivateKey(ecdsa, sourceArray);
        bytesRead = sourceArray.Length;
    }

    #region SEC1 ECPrivateKey ASN.1

    /// <summary>
    /// Decodes a SEC1 ECPrivateKey and imports it.
    /// <code>
    /// ECPrivateKey ::= SEQUENCE {
    ///     version        INTEGER { ecPrivkeyVer1(1) },
    ///     privateKey     OCTET STRING,
    ///     parameters [0] ECParameters {{ NamedCurve }} OPTIONAL,
    ///     publicKey  [1] BIT STRING OPTIONAL
    /// }
    /// </code>
    /// </summary>
    private static void ImportEcPrivateKeyCore(ECDsa ecdsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // version (1)
        byte[] privateKeyBytes = sequence.ReadOctetString();

        // Read optional parameters [0] - contains curve OID
        ECCurve curve = default;
        if (sequence.HasData && sequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            AsnReader paramsReader = sequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            string curveOid = paramsReader.ReadObjectIdentifier();
            curve = CurveFromOid(curveOid);
        }

        // Read optional public key [1]
        byte[] publicKeyBits = null;
        if (sequence.HasData && sequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
        {
            AsnReader pubKeyReader = sequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            publicKeyBits = pubKeyReader.ReadBitString(out _);
        }

        ECParameters parameters = BuildEcParameters(privateKeyBytes, publicKeyBits, curve);
        ecdsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Encodes EC parameters as SEC1 ECPrivateKey DER.
    /// </summary>
    private static byte[] EncodeEcPrivateKey(ECParameters parameters)
    {
        string curveOid = CurveToOid(parameters.Curve);
        byte[] uncompressedPoint = BuildUncompressedPoint(parameters.Q);

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        writer.WriteInteger(1); // version = ecPrivkeyVer1
        writer.WriteOctetString(parameters.D);

        // Parameters [0]
        Asn1Tag contextTag0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        writer.PushSequence(contextTag0);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence(contextTag0);

        // PublicKey [1]
        Asn1Tag contextTag1 = new Asn1Tag(TagClass.ContextSpecific, 1, true);
        writer.PushSequence(contextTag1);
        writer.WriteBitString(uncompressedPoint);
        writer.PopSequence(contextTag1);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region SubjectPublicKeyInfo (SPKI) for EC

    /// <summary>
    /// Decodes a SubjectPublicKeyInfo (SPKI) structure containing an EC public key.
    /// </summary>
    private static void ImportSubjectPublicKeyInfo(ECDsa ecdsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader spkiSequence = reader.ReadSequence();

        // AlgorithmIdentifier
        AsnReader algId = spkiSequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != EcPublicKeyOid)
            throw new CryptographicException($"Oczekiwano OID EC ({EcPublicKeyOid}), otrzymano '{oid}'.");

        string curveOid = algId.ReadObjectIdentifier();
        ECCurve curve = CurveFromOid(curveOid);

        // SubjectPublicKey BIT STRING → uncompressed EC point (04 || X || Y)
        byte[] publicKeyBits = spkiSequence.ReadBitString(out _);

        ECParameters parameters = new ECParameters
        {
            Curve = curve,
            Q = ParseUncompressedPoint(publicKeyBits, curve)
        };

        ecdsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Encodes EC public key parameters as SubjectPublicKeyInfo (SPKI) DER.
    /// </summary>
    internal static byte[] EncodeSubjectPublicKeyInfo(ECParameters parameters)
    {
        string curveOid = CurveToOid(parameters.Curve);
        byte[] uncompressedPoint = BuildUncompressedPoint(parameters.Q);

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // AlgorithmIdentifier
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // SubjectPublicKey as BIT STRING
        writer.WriteBitString(uncompressedPoint);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region PKCS#8 PrivateKeyInfo for EC

    /// <summary>
    /// Decodes a PKCS#8 PrivateKeyInfo structure and imports the EC key.
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///     version                   INTEGER,
    ///     privateKeyAlgorithm       AlgorithmIdentifier,
    ///     privateKey                OCTET STRING  -- contains SEC1 ECPrivateKey
    /// }
    /// </code>
    /// </summary>
    private static void ImportPkcs8PrivateKey(ECDsa ecdsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // version (0)

        // AlgorithmIdentifier: EC OID + curve OID
        AsnReader algId = sequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != EcPublicKeyOid)
            throw new CryptographicException($"PKCS#8 zawiera algorytm '{oid}', oczekiwano EC ({EcPublicKeyOid}).");

        string curveOid = algId.ReadObjectIdentifier();
        ECCurve curve = CurveFromOid(curveOid);

        // PrivateKey OCTET STRING → contains SEC1 ECPrivateKey (but without curve parameters)
        byte[] ecPrivateKeyDer = sequence.ReadOctetString();

        // Parse the inner SEC1 ECPrivateKey
        AsnReader ecReader = new AsnReader(ecPrivateKeyDer, AsnEncodingRules.DER);
        AsnReader ecSequence = ecReader.ReadSequence();

        ecSequence.ReadInteger(); // version (1)
        byte[] privateKeyBytes = ecSequence.ReadOctetString();

        // The public key may be in the SEC1 structure
        byte[] publicKeyBits = null;

        // Skip optional parameters [0] if present
        if (ecSequence.HasData && ecSequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            ecSequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)); // discard
        }

        // Read optional public key [1] if present
        if (ecSequence.HasData && ecSequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
        {
            AsnReader pubKeyReader = ecSequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            publicKeyBits = pubKeyReader.ReadBitString(out _);
        }

        ECParameters parameters = BuildEcParameters(privateKeyBytes, publicKeyBits, curve);
        ecdsa.ImportParameters(parameters);
    }

    #endregion

    #region EC Helpers

    /// <summary>
    /// Builds <see cref="ECParameters"/> from raw key bytes and an optional uncompressed public key point.
    /// If the public key is not provided, derives it from the private key using a temporary ECDsa instance.
    /// </summary>
    private static ECParameters BuildEcParameters(byte[] privateKeyBytes, byte[] publicKeyBits, ECCurve curve)
    {
        ECParameters parameters = new ECParameters
        {
            Curve = curve,
            D = privateKeyBytes
        };

        if (publicKeyBits != null && publicKeyBits.Length > 0)
        {
            parameters.Q = ParseUncompressedPoint(publicKeyBits, curve);
        }
        else
        {
            // Derive public key from private key by importing and re-exporting
            using ECDsa temp = ECDsa.Create(curve);
            // Import the private key with a dummy Q, then export to get real Q
            int coordSize = GetCoordSize(curve);
            parameters.Q = new ECPoint
            {
                X = new byte[coordSize],
                Y = new byte[coordSize]
            };

            try
            {
                temp.ImportParameters(parameters);
                ECParameters exported = temp.ExportParameters(false);
                parameters.Q = exported.Q;
            }
            catch
            {
                // Fallback: create with curve and import differently
                throw new CryptographicException(
                    "Nie udało się odtworzyć klucza publicznego EC z klucza prywatnego.");
            }
        }

        return parameters;
    }

    /// <summary>
    /// Parses an uncompressed EC point (0x04 || X || Y) into an <see cref="ECPoint"/>.
    /// </summary>
    internal static ECPoint ParseUncompressedPoint(byte[] point, ECCurve curve)
    {
        if (point is null || point.Length == 0)
            throw new CryptographicException("Pusty punkt EC.");

        if (point[0] != 0x04)
            throw new CryptographicException(
                $"Obsługiwany jest tylko nieskompresowany format punktu EC (0x04), otrzymano 0x{point[0]:X2}.");

        int coordSize = (point.Length - 1) / 2;
        byte[] x = new byte[coordSize];
        byte[] y = new byte[coordSize];
        Buffer.BlockCopy(point, 1, x, 0, coordSize);
        Buffer.BlockCopy(point, 1 + coordSize, y, 0, coordSize);

        return new ECPoint { X = x, Y = y };
    }

    /// <summary>
    /// Builds an uncompressed EC point (0x04 || X || Y) from an <see cref="ECPoint"/>.
    /// </summary>
    internal static byte[] BuildUncompressedPoint(ECPoint q)
    {
        byte[] point = new byte[1 + q.X.Length + q.Y.Length];
        point[0] = 0x04;
        Buffer.BlockCopy(q.X, 0, point, 1, q.X.Length);
        Buffer.BlockCopy(q.Y, 0, point, 1 + q.X.Length, q.Y.Length);
        return point;
    }

    /// <summary>
    /// Returns the coordinate size in bytes for the given curve.
    /// </summary>
    internal static int GetCoordSize(ECCurve curve)
    {
        string oid = curve.Oid?.Value;
        return oid switch
        {
            NistP256Oid => 32,
            NistP384Oid => 48,
            NistP521Oid => 66,
            _ => 32 // default to P-256
        };
    }

    /// <summary>
    /// Converts a curve OID string to an <see cref="ECCurve"/>.
    /// </summary>
    internal static ECCurve CurveFromOid(string oid)
    {
        return oid switch
        {
            NistP256Oid => ECCurve.NamedCurves.nistP256,
            NistP384Oid => ECCurve.NamedCurves.nistP384,
            NistP521Oid => ECCurve.NamedCurves.nistP521,
            _ => throw new CryptographicException($"Nieobsługiwana krzywa EC o OID '{oid}'.")
        };
    }

    /// <summary>
    /// Converts an <see cref="ECCurve"/> to its OID string.
    /// </summary>
    internal static string CurveToOid(ECCurve curve)
    {
        string oid = curve.Oid?.Value;
        if (!string.IsNullOrEmpty(oid))
            return oid;

        // Try matching by friendly name
        string name = curve.Oid?.FriendlyName;
        return name switch
        {
            "nistP256" or "ECDSA_P256" => NistP256Oid,
            "nistP384" or "ECDSA_P384" => NistP384Oid,
            "nistP521" or "ECDSA_P521" => NistP521Oid,
            _ => throw new CryptographicException(
                $"Nie można określić OID dla krzywej EC '{name}'.")
        };
    }

    #endregion
}
#endif
