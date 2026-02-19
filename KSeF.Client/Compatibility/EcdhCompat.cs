#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Reflection;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla operacji <see cref="ECDiffieHellman"/> na netstandard2.0 / .NET Framework 4.8.
/// <see cref="ECDiffieHellman"/> jest dostępny w RUNTIME na .NET Framework 4.8 (jako ECDiffieHellmanCng),
/// ale NIE jest częścią kontraktu kompilacji netstandard2.0.
/// Ta klasa wykorzystuje refleksję do uzyskania dostępu do typów dostępnych w runtime.
/// </summary>
/// <remarks>
/// Cache refleksji jest zabezpieczony barierami pamięciowymi (<see cref="Volatile"/>),
/// aby zapewnić poprawność na architekturach z weak memory model (np. ARM).
/// </remarks>
internal sealed class EcdhCompat : IDisposable
{
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    private readonly object _ecdh;        // Instancja ECDiffieHellman
    private readonly Type _ecdhType;
    private bool _disposed;

    // Buforowane informacje refleksji
    private static Type s_ecdhType;
    private static MethodInfo s_createMethod;
    private static PropertyInfo s_publicKeyProp;
    private static MethodInfo s_deriveKeyMaterialMethod;
    private static bool s_resolved;

    private EcdhCompat(object ecdhInstance)
    {
        _ecdh = ecdhInstance;
        _ecdhType = ecdhInstance.GetType();
    }

    /// <summary>
    /// Tworzy nową instancję ECDiffieHellman z krzywą P-256.
    /// </summary>
    public static EcdhCompat Create()
    {
        EnsureResolved();
        object instance = s_createMethod.Invoke(null, new object[] { ECCurve.NamedCurves.nistP256 });
        if (instance == null)
            throw new PlatformNotSupportedException("ECDiffieHellman.Create(ECCurve) zwróciło null.");
        return new EcdhCompat(instance);
    }

    /// <summary>
    /// Importuje klucz publiczny EC z ciągu SPKI zakodowanego w PEM.
    /// </summary>
    public void ImportFromPem(string pem)
    {
        if (pem == null) throw new ArgumentNullException(nameof(pem));

        byte[] der = PemHelper.DecodePem(pem, out string label);

        if (!string.Equals(label, "PUBLIC KEY", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException($"Oczekiwano bloku PEM 'PUBLIC KEY', otrzymano '{label}'.");

        ECParameters parameters = DecodeSpkiToEcParameters(der);
        ImportParameters(parameters);
    }

    /// <summary>
    /// Importuje parametry EC do bazowej instancji ECDiffieHellman.
    /// </summary>
    private void ImportParameters(ECParameters parameters)
    {
        // ECDiffieHellmanCng nie ma bezpośrednio ImportParameters,
        // ale możemy użyć ECDiffieHellman.Create() z parametrami jako obejście:
        // Tworzymy nową instancję, potem importujemy przez właściwość Key.

        // W rzeczywistości na .NET Framework 4.8, ECDiffieHellmanCng posiada ImportParameters
        // odziedziczone z ECDiffieHellman (dodane w .NET Framework 4.7)
        MethodInfo importMethod = _ecdhType.GetMethod("ImportParameters",
            BindingFlags.Public | BindingFlags.Instance,
            null, new[] { typeof(ECParameters) }, null);

        if (importMethod != null)
        {
            importMethod.Invoke(_ecdh, new object[] { parameters });
            return;
        }

        throw new PlatformNotSupportedException(
            "ECDiffieHellman.ImportParameters nie jest dostępne na tej platformie.");
    }

    /// <summary>
    /// Wyprowadza wspólny sekret przy użyciu klucza publicznego drugiej strony.
    /// </summary>
    public byte[] DeriveKeyMaterial(EcdhCompat otherPublicKey)
    {
        EnsureResolved();
        object otherPubKey = s_publicKeyProp.GetValue(otherPublicKey._ecdh);
        return (byte[])s_deriveKeyMaterialMethod.Invoke(_ecdh, new[] { otherPubKey });
    }

    /// <summary>
    /// Pobiera klucz publiczny i eksportuje go jako SubjectPublicKeyInfo w formacie DER.
    /// </summary>
    public byte[] ExportSubjectPublicKeyInfo()
    {
        // Eksportuj parametry EC i zakoduj jako SPKI
        MethodInfo exportMethod = _ecdhType.GetMethod("ExportParameters",
            BindingFlags.Public | BindingFlags.Instance,
            null, new[] { typeof(bool) }, null);

        if (exportMethod == null)
            throw new PlatformNotSupportedException("ECDiffieHellman.ExportParameters nie jest dostępne.");

        ECParameters parameters = (ECParameters)exportMethod.Invoke(_ecdh, new object[] { false });
        return EncodeEcPublicKeySpki(parameters);
    }

    private static void EnsureResolved()
    {
        PlatformGuard.EnsureWindowsCng();
        if (Volatile.Read(ref s_resolved)) return;

        // Spróbuj znaleźć ECDiffieHellman w załadowanych assembly
        s_ecdhType = typeof(ECDsa).Assembly.GetType("System.Security.Cryptography.ECDiffieHellman");

        if (s_ecdhType == null)
        {
            // Szukaj we wszystkich załadowanych assembly
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                s_ecdhType = asm.GetType("System.Security.Cryptography.ECDiffieHellman");
                if (s_ecdhType != null) break;
            }
        }

        if (s_ecdhType == null)
            throw new PlatformNotSupportedException(
                "ECDiffieHellman nie jest dostępny na tej platformie. Wymagany jest .NET Framework 4.7+.");

        s_createMethod = s_ecdhType.GetMethod("Create",
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(ECCurve) }, null);

        if (s_createMethod == null)
            throw new PlatformNotSupportedException("Nie znaleziono ECDiffieHellman.Create(ECCurve).");

        s_publicKeyProp = s_ecdhType.GetProperty("PublicKey",
            BindingFlags.Public | BindingFlags.Instance);

        // ECDiffieHellman.DeriveKeyMaterial(ECDiffieHellmanPublicKey)
        Type pubKeyType = s_ecdhType.Assembly.GetType("System.Security.Cryptography.ECDiffieHellmanPublicKey");
        if (pubKeyType != null)
        {
            s_deriveKeyMaterialMethod = s_ecdhType.GetMethod("DeriveKeyMaterial",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { pubKeyType }, null);
        }

        // Defense-in-depth: walidacja wymaganych elementów refleksji przed oznaczeniem jako resolved.
        // Bez tych kontroli `DeriveKeyMaterial()` na linii 100 rzuciłoby kryptyczne NullReferenceException.
        if (s_publicKeyProp == null)
            throw new PlatformNotSupportedException(
                "Nie znaleziono właściwości ECDiffieHellman.PublicKey. " +
                "Wymagany jest .NET Framework 4.7+ na Windows.");

        if (s_deriveKeyMaterialMethod == null)
            throw new PlatformNotSupportedException(
                "Nie znaleziono metody ECDiffieHellman.DeriveKeyMaterial. " +
                "Wymagany jest .NET Framework 4.7+ na Windows.");

        Volatile.Write(ref s_resolved, true);
    }

    #region Kodowanie/dekodowanie ASN.1 SPKI

    /// <summary>
    /// Dekoduje strukturę DER SubjectPublicKeyInfo (SPKI) do <see cref="ECParameters"/>.
    /// </summary>
    private static ECParameters DecodeSpkiToEcParameters(byte[] spki)
    {
        AsnReader reader = new AsnReader(spki, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        // AlgorithmIdentifier
        AsnReader algId = sequence.ReadSequence();
        string algOid = algId.ReadObjectIdentifier();
        if (algOid != EcPublicKeyOid)
            throw new CryptographicException($"Oczekiwano OID klucza publicznego EC, otrzymano '{algOid}'.");
        string curveOid = algId.ReadObjectIdentifier();

        ECCurve curve;
        if (curveOid == NistP256Oid)
            curve = ECCurve.NamedCurves.nistP256;
        else if (curveOid == "1.3.132.0.34")
            curve = ECCurve.NamedCurves.nistP384;
        else if (curveOid == "1.3.132.0.35")
            curve = ECCurve.NamedCurves.nistP521;
        else
            throw new CryptographicException($"Nieobsługiwany OID krzywej EC: '{curveOid}'.");

        // SubjectPublicKey BIT STRING
        byte[] publicKeyBits = sequence.ReadBitString(out int unusedBits);

        // Punkt nieskompresowany: 0x04 || X || Y
        if (publicKeyBits.Length == 0 || publicKeyBits[0] != 0x04)
            throw new CryptographicException("Obsługiwane są tylko nieskompresowane punkty EC.");

        int coordLen = (publicKeyBits.Length - 1) / 2;
        byte[] x = new byte[coordLen];
        byte[] y = new byte[coordLen];
        Buffer.BlockCopy(publicKeyBits, 1, x, 0, coordLen);
        Buffer.BlockCopy(publicKeyBits, 1 + coordLen, y, 0, coordLen);

        return new ECParameters
        {
            Curve = curve,
            Q = new ECPoint { X = x, Y = y }
        };
    }

    /// <summary>
    /// Koduje parametry klucza publicznego EC jako SubjectPublicKeyInfo (SPKI) w formacie DER.
    /// </summary>
    /// <remarks>
    /// OID krzywej jest wyznaczany z parametrów: ścieżka główna używa <c>Oid.Value</c> (zawsze wypełniony
    /// na .NET FW 4.7.2+ z CNG), fallback na <c>Oid.FriendlyName</c> z precyzyjnym dopasowaniem
    /// nazw CNG (nistP256, ECDSA_P256, ECDH_P256 itp.).
    /// </remarks>
    private static byte[] EncodeEcPublicKeySpki(ECParameters parameters)
    {
        int coordLen = parameters.Q.X.Length;
        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(parameters.Q.X, 0, point, 1, coordLen);
        Buffer.BlockCopy(parameters.Q.Y, 0, point, 1 + coordLen, coordLen);

        // Ścieżka główna: Oid.Value jest wypełniony na .NET FW 4.7.2+ z CNG
        string curveOid;
        if (parameters.Curve.Oid?.Value != null)
        {
            curveOid = parameters.Curve.Oid.Value;
        }
        else if (parameters.Curve.Oid?.FriendlyName != null)
        {
            // Fallback: precyzyjne dopasowanie nazw CNG (zamiast fragile Contains)
            string friendly = parameters.Curve.Oid.FriendlyName;
            curveOid = friendly switch
            {
                "nistP256" or "ECDSA_P256" or "ECDH_P256" => NistP256Oid,
                "nistP384" or "ECDSA_P384" or "ECDH_P384" => "1.3.132.0.34",
                "nistP521" or "ECDSA_P521" or "ECDH_P521" => "1.3.132.0.35",
                _ => throw new CryptographicException(
                    $"Nie można określić OID dla krzywej EC ECDH o nazwie '{friendly}'.")
            };
        }
        else
        {
            throw new CryptographicException(
                "Nie można określić OID krzywej EC — brak Oid.Value i Oid.FriendlyName w parametrach.");
        }

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // AlgorithmIdentifier
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // SubjectPublicKey BIT STRING
        writer.WriteBitString(point);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_ecdh is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
#endif
