#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <see cref="CertificateRequest"/> and <c>CreateSelfSigned</c>,
/// which are not available on netstandard2.0 / .NET Framework 4.8.
/// Builds a self-signed X.509 v3 certificate using raw ASN.1 encoding.
/// </summary>
internal static class SelfSignedCertificateCompat
{
    // OIDs
    private const string Sha256WithRsaOid = "1.2.840.113549.1.1.11"; // sha256WithRSAEncryption
    private const string RsaSsaPssOid = "1.2.840.113549.1.1.10";     // id-RSASSA-PSS
    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";       // id-sha256
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";  // rsaEncryption
    private const string MgfSha256Oid = "1.2.840.113549.1.1.8";      // id-mgf1
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";       // id-ecPublicKey
    private const string EcdsaWithSha256Oid = "1.2.840.10045.4.3.2"; // ecdsa-with-SHA256
    private const string NistP256Oid = "1.2.840.10045.3.1.7";        // secp256r1

    /// <summary>
    /// Creates a self-signed X.509 v3 certificate with an RSA key, using RSA-PSS signature.
    /// </summary>
    /// <param name="subjectDN">The distinguished name string (e.g., "2.5.4.3=CN, 2.5.4.6=PL").</param>
    /// <param name="notBefore">Certificate validity start.</param>
    /// <param name="notAfter">Certificate validity end.</param>
    /// <returns>A self-signed <see cref="X509Certificate2"/> with private key.</returns>
    public static X509Certificate2 CreateSelfSignedRsa(
        string subjectDN,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        // RSACng supports PSS signing; RSACryptoServiceProvider (from RSA.Create()) does not.
        RSACng rsa = new RSACng(2048);
        byte[] tbsCert = BuildTbsCertificate(subjectDN, rsa, notBefore, notAfter, isEcdsa: false);
        byte[] signature = rsa.SignData(tbsCert, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        byte[] certDer = WrapSignedCertificate(tbsCert, signature, isEcdsa: false);

        // CopyWithPrivateKey on .NET Framework may wrap the key as RSACryptoServiceProvider
        // which doesn't support PSS. Export to PFX and reimport to preserve CNG key type.
        X509Certificate2 pubCert = new X509Certificate2(certDer);
        X509Certificate2 certWithKey = pubCert.CopyWithPrivateKey(rsa);
        byte[] pfxBytes = certWithKey.Export(X509ContentType.Pfx, string.Empty);
        pubCert.Dispose();
        certWithKey.Dispose();
        return new X509Certificate2(pfxBytes, string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    /// <summary>
    /// Creates a self-signed X.509 v3 certificate with an ECDsa (P-256) key.
    /// </summary>
    /// <param name="subjectDN">The distinguished name string.</param>
    /// <param name="notBefore">Certificate validity start.</param>
    /// <param name="notAfter">Certificate validity end.</param>
    /// <returns>A self-signed <see cref="X509Certificate2"/> with private key.</returns>
    public static X509Certificate2 CreateSelfSignedEcdsa(
        string subjectDN,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] tbsCert = BuildTbsCertificate(subjectDN, ecdsa, notBefore, notAfter, isEcdsa: true);
        // ECDsa.SignData on .NET Framework returns IEEE P1363 format (r||s).
        // X.509 certificates require DER-encoded ECDSA signature.
        byte[] ieeeSignature = ecdsa.SignData(tbsCert, HashAlgorithmName.SHA256);
        byte[] derSignature = ConvertIeeeP1363ToDer(ieeeSignature);
        byte[] certDer = WrapSignedCertificate(tbsCert, derSignature, isEcdsa: true);

        X509Certificate2 pubCert = new X509Certificate2(certDer);
        return pubCert.CopyWithPrivateKey(ecdsa);
    }

    /// <summary>
    /// Builds the TBSCertificate ASN.1 structure (RFC 5280 §4.1.2).
    /// </summary>
    private static byte[] BuildTbsCertificate(
        string subjectDN,
        AsymmetricAlgorithm key,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        bool isEcdsa)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence(); // TBSCertificate

        // version [0] EXPLICIT INTEGER (v3 = 2)
        Asn1Tag contextTag0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSequence(contextTag0);
        writer.WriteInteger(2);
        writer.PopSequence(contextTag0);

        // serialNumber INTEGER
        byte[] serial = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serial);
        }
        serial[0] &= 0x7F; // Ensure positive
        writer.WriteInteger(serial);

        // signature AlgorithmIdentifier
        WriteSignatureAlgorithm(writer, isEcdsa);

        // issuer Name (same as subject for self-signed)
        byte[] nameBytes = EncodeDistinguishedName(subjectDN);
        writer.WriteEncodedValue(nameBytes);

        // validity Validity
        writer.PushSequence();
        writer.WriteUtcTime(notBefore);
        writer.WriteUtcTime(notAfter);
        writer.PopSequence();

        // subject Name
        writer.WriteEncodedValue(nameBytes);

        // subjectPublicKeyInfo
        if (isEcdsa)
        {
            WriteEcdsaPublicKeyInfo(writer, (ECDsa)key);
        }
        else
        {
            WriteRsaPublicKeyInfo(writer, (RSA)key);
        }

        writer.PopSequence(); // end TBSCertificate
        return writer.Encode();
    }

    /// <summary>
    /// Wraps TBSCertificate + signature into the final Certificate ASN.1 structure.
    /// </summary>
    private static byte[] WrapSignedCertificate(byte[] tbsCert, byte[] signature, bool isEcdsa)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence(); // Certificate

        // tbsCertificate (already DER-encoded SEQUENCE)
        writer.WriteEncodedValue(tbsCert);

        // signatureAlgorithm
        WriteSignatureAlgorithm(writer, isEcdsa);

        // signatureValue BIT STRING
        writer.WriteBitString(signature);

        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Writes the AlgorithmIdentifier for the signature algorithm.
    /// </summary>
    private static void WriteSignatureAlgorithm(AsnWriter writer, bool isEcdsa)
    {
        if (isEcdsa)
        {
            // ecdsa-with-SHA256: SEQUENCE { OID }
            writer.PushSequence();
            writer.WriteObjectIdentifier(EcdsaWithSha256Oid);
            writer.PopSequence();
        }
        else
        {
            // RSASSA-PSS with SHA-256, MGF1-SHA256, saltLength=32
            WriteRsaPssAlgorithmIdentifier(writer);
        }
    }

    /// <summary>
    /// Writes the RSASSA-PSS AlgorithmIdentifier with SHA-256 parameters.
    /// <code>
    /// SEQUENCE {
    ///   OID id-RSASSA-PSS,
    ///   SEQUENCE {              -- RSASSA-PSS-params
    ///     [0] SEQUENCE { OID sha256 },
    ///     [1] SEQUENCE { OID id-mgf1, SEQUENCE { OID sha256 } },
    ///     [2] INTEGER 32
    ///   }
    /// }
    /// </code>
    /// </summary>
    private static void WriteRsaPssAlgorithmIdentifier(AsnWriter writer)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaSsaPssOid);

        // RSASSA-PSS-params
        writer.PushSequence();

        // [0] hashAlgorithm
        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSequence(ctx0);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
        writer.PopSequence();
        writer.PopSequence(ctx0);

        // [1] maskGenAlgorithm
        Asn1Tag ctx1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
        writer.PushSequence(ctx1);
        writer.PushSequence();
        writer.WriteObjectIdentifier(MgfSha256Oid);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
        writer.PopSequence();
        writer.PopSequence();
        writer.PopSequence(ctx1);

        // [2] saltLength
        Asn1Tag ctx2 = new Asn1Tag(TagClass.ContextSpecific, 2, isConstructed: true);
        writer.PushSequence(ctx2);
        writer.WriteInteger(32);
        writer.PopSequence(ctx2);

        writer.PopSequence(); // end RSASSA-PSS-params
        writer.PopSequence(); // end AlgorithmIdentifier
    }

    /// <summary>
    /// Writes SubjectPublicKeyInfo for RSA.
    /// </summary>
    private static void WriteRsaPublicKeyInfo(AsnWriter writer, RSA rsa)
    {
        RSAParameters p = rsa.ExportParameters(false);

        // Build the RSAPublicKey (inner BIT STRING content)
        AsnWriter pubKeyWriter = new AsnWriter(AsnEncodingRules.DER);
        pubKeyWriter.PushSequence();
        pubKeyWriter.WriteIntegerUnsigned(p.Modulus);
        pubKeyWriter.WriteIntegerUnsigned(p.Exponent);
        pubKeyWriter.PopSequence();
        byte[] rsaPubKey = pubKeyWriter.Encode();

        // SubjectPublicKeyInfo
        writer.PushSequence();

        // algorithm AlgorithmIdentifier { rsaEncryption, NULL }
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();

        // subjectPublicKey BIT STRING
        writer.WriteBitString(rsaPubKey);

        writer.PopSequence();
    }

    /// <summary>
    /// Writes SubjectPublicKeyInfo for ECDsa (P-256).
    /// </summary>
    private static void WriteEcdsaPublicKeyInfo(AsnWriter writer, ECDsa ecdsa)
    {
        ECParameters p = ecdsa.ExportParameters(false);
        int coordLen = p.Q.X!.Length;

        // Uncompressed point: 0x04 || X || Y
        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(p.Q.X!, 0, point, 1, coordLen);
        Buffer.BlockCopy(p.Q.Y!, 0, point, 1 + coordLen, coordLen);

        // SubjectPublicKeyInfo
        writer.PushSequence();

        // algorithm AlgorithmIdentifier { id-ecPublicKey, namedCurve OID }
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(NistP256Oid);
        writer.PopSequence();

        // subjectPublicKey BIT STRING
        writer.WriteBitString(point);

        writer.PopSequence();
    }

    /// <summary>
    /// Encodes a comma-separated DN string into ASN.1 DER Name (RDNSequence).
    /// Supports OID=Value format (e.g., "2.5.4.3=Test, 2.5.4.6=PL").
    /// </summary>
    private static byte[] EncodeDistinguishedName(string dn)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence(); // RDNSequence

        string[] parts = dn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            int eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0) continue;

            string oid = trimmed.Substring(0, eqIndex).Trim();
            string value = trimmed.Substring(eqIndex + 1).Trim();

            // RelativeDistinguishedName SET OF
            writer.PushSetOf();
            // AttributeTypeAndValue SEQUENCE
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, value);
            writer.PopSequence();
            writer.PopSetOf();
        }

        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Converts an ECDSA signature from IEEE P1363 (r||s) to DER format (SEQUENCE { INTEGER r, INTEGER s }).
    /// </summary>
    private static byte[] ConvertIeeeP1363ToDer(byte[] ieeeSignature)
    {
        int halfLen = ieeeSignature.Length / 2;
        byte[] r = ieeeSignature.AsSpan(0, halfLen).ToArray();
        byte[] s = ieeeSignature.AsSpan(halfLen, halfLen).ToArray();

        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteIntegerUnsigned(TrimLeadingZeros(r));
        writer.WriteIntegerUnsigned(TrimLeadingZeros(s));
        writer.PopSequence();
        return writer.Encode();
    }

    private static byte[] TrimLeadingZeros(byte[] data)
    {
        int start = 0;
        while (start < data.Length - 1 && data[start] == 0)
            start++;
        if (start == 0) return data;
        byte[] trimmed = new byte[data.Length - start];
        Buffer.BlockCopy(data, start, trimmed, 0, trimmed.Length);
        return trimmed;
    }
}
#endif
