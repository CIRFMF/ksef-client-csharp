#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfillowe metody rozszerzające dla operacji importu/eksportu kluczy <see cref="ECDsa"/>
/// dostępnych od .NET 5 / .NET Core 3.0.
/// Używa <see cref="System.Formats.Asn1"/> do kodowania/dekodowania ASN.1 DER.
/// </summary>
internal static class EcdsaCompat
{
    /// <summary>OID algorytmu klucza publicznego EC (1.2.840.10045.2.1).</summary>
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";

    /// <summary>OID krzywej NIST P-256 (1.2.840.10045.3.1.7).</summary>
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    /// <summary>OID krzywej NIST P-384 (1.3.132.0.34).</summary>
    private const string NistP384Oid = "1.3.132.0.34";

    /// <summary>OID krzywej NIST P-521 (1.3.132.0.35).</summary>
    private const string NistP521Oid = "1.3.132.0.35";

    /// <summary>
    /// Importuje klucz ECDsa z ciągu zakodowanego w PEM.
    /// Polyfill dla <c>ECDsa.ImportFromPem(ReadOnlySpan&lt;char&gt;)</c> dostępnego od .NET 5.
    /// Obsługuje: EC PRIVATE KEY (SEC1), PRIVATE KEY (PKCS#8), PUBLIC KEY (SPKI).
    /// </summary>
    /// <param name="ecdsa">Instancja ECDsa, do której importowany jest klucz.</param>
    /// <param name="input">Klucz zakodowany w PEM.</param>
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
    /// Importuje klucz ECDsa z zaszyfrowanego ciągu zakodowanego w PEM.
    /// Polyfill dla <c>ECDsa.ImportFromEncryptedPem(ReadOnlySpan&lt;char&gt;, ReadOnlySpan&lt;char&gt;)</c> dostępnego od .NET 5.
    /// </summary>
    /// <param name="ecdsa">Instancja ECDsa, do której importowany jest klucz.</param>
    /// <param name="input">Zaszyfrowany klucz zakodowany w PEM.</param>
    /// <param name="password">Hasło do odszyfrowania klucza.</param>
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
    /// Eksportuje klucz prywatny EC w formacie SEC1 ECPrivateKey DER.
    /// Polyfill dla <c>ECDsa.ExportECPrivateKey()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">Instancja ECDsa, której klucz ma być wyeksportowany.</param>
    /// <returns>Tablica bajtów zawierająca klucz prywatny zakodowany w SEC1 DER.</returns>
    public static byte[] ExportECPrivateKey(this ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(true);
        return EncodeEcPrivateKey(parameters);
    }

    /// <summary>
    /// Eksportuje klucz publiczny ECDsa w formacie SubjectPublicKeyInfo (SPKI) DER.
    /// Polyfill dla <c>ECDsa.ExportSubjectPublicKeyInfo()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">Instancja ECDsa, której klucz ma być wyeksportowany.</param>
    /// <returns>Tablica bajtów zawierająca klucz publiczny zakodowany w SPKI DER.</returns>
    public static byte[] ExportSubjectPublicKeyInfo(this ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(false);
        return EncodeSubjectPublicKeyInfo(parameters);
    }

    /// <summary>
    /// Importuje klucz prywatny PKCS#8 PrivateKeyInfo z tablicy bajtów zakodowanej w DER.
    /// Polyfill dla <c>ECDsa.ImportPkcs8PrivateKey(ReadOnlySpan&lt;byte&gt;, out int)</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="ecdsa">Instancja ECDsa, do której importowany jest klucz.</param>
    /// <param name="source">Dane klucza prywatnego PKCS#8 zakodowane w DER.</param>
    /// <param name="bytesRead">Liczba bajtów odczytanych z <paramref name="source"/>.</param>
    public static void ImportPkcs8PrivateKey(this ECDsa ecdsa, ReadOnlySpan<byte> source, out int bytesRead)
    {
        byte[] sourceArray = source.ToArray();
        ImportPkcs8PrivateKey(ecdsa, sourceArray);
        bytesRead = sourceArray.Length;
    }

    #region SEC1 ECPrivateKey ASN.1

    /// <summary>
    /// Dekoduje SEC1 ECPrivateKey i importuje go.
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

        sequence.ReadInteger(); // wersja (1)
        byte[] privateKeyBytes = sequence.ReadOctetString();

        // Odczytaj opcjonalne parametry [0] — zawierają OID krzywej
        ECCurve curve = default;
        if (sequence.HasData && sequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            AsnReader paramsReader = sequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            string curveOid = paramsReader.ReadObjectIdentifier();
            curve = CurveFromOid(curveOid);
        }

        // Odczytaj opcjonalny klucz publiczny [1]
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
    /// Koduje parametry EC jako SEC1 ECPrivateKey DER.
    /// </summary>
    private static byte[] EncodeEcPrivateKey(ECParameters parameters)
    {
        string curveOid = CurveToOid(parameters.Curve);
        byte[] uncompressedPoint = BuildUncompressedPoint(parameters.Q);

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        writer.WriteInteger(1); // wersja = ecPrivkeyVer1
        writer.WriteOctetString(parameters.D);

        // Parametry [0]
        Asn1Tag contextTag0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        writer.PushSequence(contextTag0);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence(contextTag0);

        // Klucz publiczny [1]
        Asn1Tag contextTag1 = new Asn1Tag(TagClass.ContextSpecific, 1, true);
        writer.PushSequence(contextTag1);
        writer.WriteBitString(uncompressedPoint);
        writer.PopSequence(contextTag1);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region SubjectPublicKeyInfo (SPKI) dla EC

    /// <summary>
    /// Dekoduje strukturę SubjectPublicKeyInfo (SPKI) zawierającą klucz publiczny EC.
    /// </summary>
    private static void ImportSubjectPublicKeyInfo(ECDsa ecdsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader spkiSequence = reader.ReadSequence();

        // Identyfikator algorytmu
        AsnReader algId = spkiSequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != EcPublicKeyOid)
            throw new CryptographicException($"Oczekiwano OID EC ({EcPublicKeyOid}), otrzymano '{oid}'.");

        string curveOid = algId.ReadObjectIdentifier();
        ECCurve curve = CurveFromOid(curveOid);

        // SubjectPublicKey BIT STRING → nieskompresowany punkt EC (04 || X || Y)
        byte[] publicKeyBits = spkiSequence.ReadBitString(out _);

        ECParameters parameters = new ECParameters
        {
            Curve = curve,
            Q = ParseUncompressedPoint(publicKeyBits, curve)
        };

        ecdsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Koduje parametry klucza publicznego EC jako SubjectPublicKeyInfo (SPKI) DER.
    /// </summary>
    internal static byte[] EncodeSubjectPublicKeyInfo(ECParameters parameters)
    {
        string curveOid = CurveToOid(parameters.Curve);
        byte[] uncompressedPoint = BuildUncompressedPoint(parameters.Q);

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // Identyfikator algorytmu
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // SubjectPublicKey jako BIT STRING
        writer.WriteBitString(uncompressedPoint);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region PKCS#8 PrivateKeyInfo dla EC

    /// <summary>
    /// Dekoduje strukturę PKCS#8 PrivateKeyInfo i importuje klucz EC.
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///     version                   INTEGER,
    ///     privateKeyAlgorithm       AlgorithmIdentifier,
    ///     privateKey                OCTET STRING  -- zawiera SEC1 ECPrivateKey
    /// }
    /// </code>
    /// </summary>
    private static void ImportPkcs8PrivateKey(ECDsa ecdsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // wersja (0)

        // AlgorithmIdentifier: OID EC + OID krzywej
        AsnReader algId = sequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != EcPublicKeyOid)
            throw new CryptographicException($"PKCS#8 zawiera algorytm '{oid}', oczekiwano EC ({EcPublicKeyOid}).");

        string curveOid = algId.ReadObjectIdentifier();
        ECCurve curve = CurveFromOid(curveOid);

        // PrivateKey OCTET STRING → zawiera SEC1 ECPrivateKey (bez parametrów krzywej)
        byte[] ecPrivateKeyDer = sequence.ReadOctetString();

        // Parsuj wewnętrzny SEC1 ECPrivateKey
        AsnReader ecReader = new AsnReader(ecPrivateKeyDer, AsnEncodingRules.DER);
        AsnReader ecSequence = ecReader.ReadSequence();

        ecSequence.ReadInteger(); // wersja (1)
        byte[] privateKeyBytes = ecSequence.ReadOctetString();

        // Klucz publiczny może znajdować się w strukturze SEC1
        byte[] publicKeyBits = null;

        // Pomiń opcjonalne parametry [0] jeśli obecne
        if (ecSequence.HasData && ecSequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            ecSequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)); // odrzuć
        }

        // Odczytaj opcjonalny klucz publiczny [1] jeśli obecny
        if (ecSequence.HasData && ecSequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
        {
            AsnReader pubKeyReader = ecSequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            publicKeyBits = pubKeyReader.ReadBitString(out _);
        }

        ECParameters parameters = BuildEcParameters(privateKeyBytes, publicKeyBits, curve);
        ecdsa.ImportParameters(parameters);
    }

    #endregion

    #region Pomocniki EC

    /// <summary>
    /// Buduje <see cref="ECParameters"/> z surowych bajtów klucza i opcjonalnego nieskompresowanego punktu klucza publicznego.
    /// Jeśli klucz publiczny nie jest podany, wyprowadza go z klucza prywatnego za pomocą tymczasowej instancji ECDsa.
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
            // Wyprowadź klucz publiczny z prywatnego przez import i reeksport
            using ECDsa temp = ECDsa.Create(curve);
            // Importuj klucz prywatny z fikcyjnym Q, potem eksportuj aby uzyskać prawdziwy Q
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
                // Awaryjnie: nie udało się odtworzyć klucza publicznego
                throw new CryptographicException(
                    "Nie udało się odtworzyć klucza publicznego EC z klucza prywatnego.");
            }
        }

        return parameters;
    }

    /// <summary>
    /// Parsuje nieskompresowany punkt EC (0x04 || X || Y) do <see cref="ECPoint"/>.
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
    /// Buduje nieskompresowany punkt EC (0x04 || X || Y) z <see cref="ECPoint"/>.
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
    /// Zwraca rozmiar współrzędnej w bajtach dla podanej krzywej.
    /// </summary>
    internal static int GetCoordSize(ECCurve curve)
    {
        string oid = curve.Oid?.Value;
        return oid switch
        {
            NistP256Oid => 32,
            NistP384Oid => 48,
            NistP521Oid => 66,
            _ => 32 // domyślnie P-256
        };
    }

    /// <summary>
    /// Konwertuje ciąg OID krzywej na <see cref="ECCurve"/>.
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
    /// Konwertuje <see cref="ECCurve"/> na ciąg OID.
    /// </summary>
    internal static string CurveToOid(ECCurve curve)
    {
        string oid = curve.Oid?.Value;
        if (!string.IsNullOrEmpty(oid))
            return oid;

        // Spróbuj dopasować po przyjaznej nazwie
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
