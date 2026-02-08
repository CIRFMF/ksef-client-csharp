#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Reflection;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <see cref="ECDiffieHellman"/> operations on netstandard2.0 / .NET Framework 4.8.
/// <see cref="ECDiffieHellman"/> is available at RUNTIME on .NET Framework 4.8 (as ECDiffieHellmanCng)
/// but is NOT part of the netstandard2.0 compile-time contract.
/// This class uses reflection to access the runtime types.
/// </summary>
internal sealed class EcdhCompat : IDisposable
{
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    private readonly object _ecdh;        // ECDiffieHellman instance
    private readonly Type _ecdhType;
    private bool _disposed;

    // Cached reflection info
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
    /// Creates a new ECDiffieHellman instance with the P-256 curve.
    /// </summary>
    public static EcdhCompat Create()
    {
        EnsureResolved();
        object instance = s_createMethod.Invoke(null, new object[] { ECCurve.NamedCurves.nistP256 });
        if (instance == null)
            throw new PlatformNotSupportedException("ECDiffieHellman.Create(ECCurve) returned null.");
        return new EcdhCompat(instance);
    }

    /// <summary>
    /// Imports an EC public key from a PEM-encoded SPKI string.
    /// </summary>
    public void ImportFromPem(string pem)
    {
        if (pem == null) throw new ArgumentNullException(nameof(pem));

        byte[] der = PemHelper.DecodePem(pem, out string label);

        if (!string.Equals(label, "PUBLIC KEY", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException($"Expected 'PUBLIC KEY' PEM block, got '{label}'.");

        ECParameters parameters = DecodeSpkiToEcParameters(der);
        ImportParameters(parameters);
    }

    /// <summary>
    /// Imports EC parameters into the underlying ECDiffieHellman instance.
    /// </summary>
    private void ImportParameters(ECParameters parameters)
    {
        // ECDiffieHellmanCng doesn't have ImportParameters directly,
        // but we can use ECDiffieHellman.Create() with parameters via a workaround:
        // Create a new instance, then import via the Key property.

        // Actually, on .NET Framework 4.8, ECDiffieHellmanCng has ImportParameters
        // inherited from ECDiffieHellman (added in .NET Framework 4.7)
        MethodInfo importMethod = _ecdhType.GetMethod("ImportParameters",
            BindingFlags.Public | BindingFlags.Instance,
            null, new[] { typeof(ECParameters) }, null);

        if (importMethod != null)
        {
            importMethod.Invoke(_ecdh, new object[] { parameters });
            return;
        }

        throw new PlatformNotSupportedException(
            "ECDiffieHellman.ImportParameters is not available on this platform.");
    }

    /// <summary>
    /// Derives a shared secret using the other party's public key.
    /// </summary>
    public byte[] DeriveKeyMaterial(EcdhCompat otherPublicKey)
    {
        EnsureResolved();
        object otherPubKey = s_publicKeyProp.GetValue(otherPublicKey._ecdh);
        return (byte[])s_deriveKeyMaterialMethod.Invoke(_ecdh, new[] { otherPubKey });
    }

    /// <summary>
    /// Gets the public key and exports it as SubjectPublicKeyInfo DER.
    /// </summary>
    public byte[] ExportSubjectPublicKeyInfo()
    {
        // Export EC parameters and encode as SPKI
        MethodInfo exportMethod = _ecdhType.GetMethod("ExportParameters",
            BindingFlags.Public | BindingFlags.Instance,
            null, new[] { typeof(bool) }, null);

        if (exportMethod == null)
            throw new PlatformNotSupportedException("ECDiffieHellman.ExportParameters not available.");

        ECParameters parameters = (ECParameters)exportMethod.Invoke(_ecdh, new object[] { false });
        return EncodeEcPublicKeySpki(parameters);
    }

    private static void EnsureResolved()
    {
        if (s_resolved) return;

        // Try to find ECDiffieHellman in loaded assemblies
        s_ecdhType = typeof(ECDsa).Assembly.GetType("System.Security.Cryptography.ECDiffieHellman");

        if (s_ecdhType == null)
        {
            // Search all loaded assemblies
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                s_ecdhType = asm.GetType("System.Security.Cryptography.ECDiffieHellman");
                if (s_ecdhType != null) break;
            }
        }

        if (s_ecdhType == null)
            throw new PlatformNotSupportedException(
                "ECDiffieHellman is not available on this platform. .NET Framework 4.7+ is required.");

        s_createMethod = s_ecdhType.GetMethod("Create",
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(ECCurve) }, null);

        if (s_createMethod == null)
            throw new PlatformNotSupportedException("ECDiffieHellman.Create(ECCurve) not found.");

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

        s_resolved = true;
    }

    #region ASN.1 SPKI encoding/decoding

    /// <summary>
    /// Decodes a SubjectPublicKeyInfo (SPKI) DER structure to ECParameters.
    /// </summary>
    private static ECParameters DecodeSpkiToEcParameters(byte[] spki)
    {
        AsnReader reader = new AsnReader(spki, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        // AlgorithmIdentifier
        AsnReader algId = sequence.ReadSequence();
        string algOid = algId.ReadObjectIdentifier();
        if (algOid != EcPublicKeyOid)
            throw new CryptographicException($"Expected EC public key OID, got '{algOid}'.");
        string curveOid = algId.ReadObjectIdentifier();

        ECCurve curve;
        if (curveOid == NistP256Oid)
            curve = ECCurve.NamedCurves.nistP256;
        else if (curveOid == "1.3.132.0.34")
            curve = ECCurve.NamedCurves.nistP384;
        else if (curveOid == "1.3.132.0.35")
            curve = ECCurve.NamedCurves.nistP521;
        else
            throw new CryptographicException($"Unsupported EC curve OID: '{curveOid}'.");

        // SubjectPublicKey BIT STRING
        byte[] publicKeyBits = sequence.ReadBitString(out int unusedBits);

        // Uncompressed point: 0x04 || X || Y
        if (publicKeyBits.Length == 0 || publicKeyBits[0] != 0x04)
            throw new CryptographicException("Only uncompressed EC points are supported.");

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
    /// Encodes EC public key parameters as SubjectPublicKeyInfo (SPKI) DER.
    /// </summary>
    private static byte[] EncodeEcPublicKeySpki(ECParameters parameters)
    {
        int coordLen = parameters.Q.X.Length;
        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(parameters.Q.X, 0, point, 1, coordLen);
        Buffer.BlockCopy(parameters.Q.Y, 0, point, 1 + coordLen, coordLen);

        string curveOid = NistP256Oid; // default
        if (parameters.Curve.Oid?.Value != null)
        {
            curveOid = parameters.Curve.Oid.Value;
        }
        else if (parameters.Curve.Oid?.FriendlyName != null)
        {
            string friendly = parameters.Curve.Oid.FriendlyName;
            if (friendly.Contains("256") || friendly.Contains("P256"))
                curveOid = NistP256Oid;
            else if (friendly.Contains("384") || friendly.Contains("P384"))
                curveOid = "1.3.132.0.34";
            else if (friendly.Contains("521") || friendly.Contains("P521"))
                curveOid = "1.3.132.0.35";
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
