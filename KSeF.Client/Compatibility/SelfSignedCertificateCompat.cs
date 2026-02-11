#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla <see cref="CertificateRequest"/> i <c>CreateSelfSigned</c>,
/// które nie są dostępne na netstandard2.0 / .NET Framework 4.8.
/// Buduje samopodpisany certyfikat X.509 v3 przy użyciu surowego kodowania ASN.1.
/// </summary>
internal static class SelfSignedCertificateCompat
{
    // OID-y
    private const string Sha256WithRsaOid = "1.2.840.113549.1.1.11"; // sha256WithRSAEncryption
    private const string RsaSsaPssOid = "1.2.840.113549.1.1.10";     // id-RSASSA-PSS
    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";       // id-sha256
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";  // rsaEncryption
    private const string MgfSha256Oid = "1.2.840.113549.1.1.8";      // id-mgf1
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";       // id-ecPublicKey
    private const string EcdsaWithSha256Oid = "1.2.840.10045.4.3.2"; // ecdsa-with-SHA256
    private const string NistP256Oid = "1.2.840.10045.3.1.7";        // secp256r1

    /// <summary>
    /// Tworzy samopodpisany certyfikat X.509 v3 z kluczem RSA, przy użyciu podpisu RSA-PSS.
    /// </summary>
    /// <param name="subjectDN">Ciąg wyróżnionej nazwy (np. "2.5.4.3=CN, 2.5.4.6=PL").</param>
    /// <param name="notBefore">Początek ważności certyfikatu.</param>
    /// <param name="notAfter">Koniec ważności certyfikatu.</param>
    /// <returns>Samopodpisany <see cref="X509Certificate2"/> z kluczem prywatnym.</returns>
    public static X509Certificate2 CreateSelfSignedRsa(
        string subjectDN,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        PlatformGuard.EnsureWindowsCng();
        // RSACng obsługuje podpisywanie PSS; RSACryptoServiceProvider (z RSA.Create()) nie obsługuje.
        // Deklaracja using zapewnia zwolnienie CNG handle po zakończeniu metody.
        using RSACng rsa = new RSACng(2048);
        byte[] tbsCert = BuildTbsCertificate(subjectDN, rsa, notBefore, notAfter, isEcdsa: false);
        byte[] signature = rsa.SignData(tbsCert, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        byte[] certDer = WrapSignedCertificate(tbsCert, signature, isEcdsa: false);

        // CopyWithPrivateKey na .NET Framework może opakować klucz jako RSACryptoServiceProvider,
        // który nie obsługuje PSS. Eksportuj do PFX i reimportuj, aby zachować typ klucza CNG.
        X509Certificate2 pubCert = new X509Certificate2(certDer);
        X509Certificate2 certWithKey = pubCert.CopyWithPrivateKey(rsa);
        byte[] pfxBytes = certWithKey.Export(X509ContentType.Pfx, string.Empty);
        pubCert.Dispose();
        certWithKey.Dispose();
        return new X509Certificate2(pfxBytes, string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    /// <summary>
    /// Tworzy samopodpisany certyfikat X.509 v3 z kluczem ECDsa (P-256).
    /// </summary>
    /// <param name="subjectDN">Ciąg wyróżnionej nazwy.</param>
    /// <param name="notBefore">Początek ważności certyfikatu.</param>
    /// <param name="notAfter">Koniec ważności certyfikatu.</param>
    /// <returns>Samopodpisany <see cref="X509Certificate2"/> z kluczem prywatnym.</returns>
    public static X509Certificate2 CreateSelfSignedEcdsa(
        string subjectDN,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        PlatformGuard.EnsureWindowsCng();
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] tbsCert = BuildTbsCertificate(subjectDN, ecdsa, notBefore, notAfter, isEcdsa: true);
        // ECDsa.SignData na .NET Framework zwraca format IEEE P1363 (r||s).
        // Certyfikaty X.509 wymagają podpisu ECDSA zakodowanego w DER.
        byte[] ieeeSignature = ecdsa.SignData(tbsCert, HashAlgorithmName.SHA256);
        byte[] derSignature = ConvertIeeeP1363ToDer(ieeeSignature);
        byte[] certDer = WrapSignedCertificate(tbsCert, derSignature, isEcdsa: true);

        X509Certificate2 pubCert = new X509Certificate2(certDer);
        return pubCert.CopyWithPrivateKey(ecdsa);
    }

    /// <summary>
    /// Buduje strukturę ASN.1 TBSCertificate (RFC 5280 §4.1.2).
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
        serial[0] &= 0x7F; // Zapewnij wartość dodatnią
        writer.WriteInteger(serial);

        // signature AlgorithmIdentifier
        WriteSignatureAlgorithm(writer, isEcdsa);

        // issuer Name (taki sam jak subject dla certyfikatu samopodpisanego)
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

        writer.PopSequence(); // koniec TBSCertificate
        return writer.Encode();
    }

    /// <summary>
    /// Opakowuje TBSCertificate + podpis w finalną strukturę ASN.1 Certificate.
    /// </summary>
    private static byte[] WrapSignedCertificate(byte[] tbsCert, byte[] signature, bool isEcdsa)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence(); // Certificate

        // tbsCertificate (już zakodowany w DER jako SEQUENCE)
        writer.WriteEncodedValue(tbsCert);

        // signatureAlgorithm
        WriteSignatureAlgorithm(writer, isEcdsa);

        // signatureValue BIT STRING
        writer.WriteBitString(signature);

        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Zapisuje AlgorithmIdentifier dla algorytmu podpisu.
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
            // RSASSA-PSS z SHA-256, MGF1-SHA256, saltLength=32
            WriteRsaPssAlgorithmIdentifier(writer);
        }
    }

    /// <summary>
    /// Zapisuje AlgorithmIdentifier RSASSA-PSS z parametrami SHA-256 (RFC 4055 §2.1).
    /// <code>
    /// SEQUENCE {
    ///   OID id-RSASSA-PSS,
    ///   SEQUENCE {              -- RSASSA-PSS-params
    ///     [0] SEQUENCE { OID sha256, NULL },
    ///     [1] SEQUENCE { OID id-mgf1, SEQUENCE { OID sha256, NULL } },
    ///     [2] INTEGER 32
    ///   }
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// Per RFC 4055 §2.1: AlgorithmIdentifier dla SHA-256 wewnątrz RSASSA-PSS-params
    /// MUSI zawierać NULL jako parametr algorytmu haszowania.
    /// </remarks>
    private static void WriteRsaPssAlgorithmIdentifier(AsnWriter writer)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaSsaPssOid);

        // RSASSA-PSS-params
        writer.PushSequence();

        // [0] hashAlgorithm — AlgorithmIdentifier { sha256, NULL } per RFC 4055 §2.1
        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSequence(ctx0);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
        writer.WriteNull();
        writer.PopSequence();
        writer.PopSequence(ctx0);

        // [1] maskGenAlgorithm — SEQUENCE { id-mgf1, AlgorithmIdentifier { sha256, NULL } }
        Asn1Tag ctx1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
        writer.PushSequence(ctx1);
        writer.PushSequence();
        writer.WriteObjectIdentifier(MgfSha256Oid);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
        writer.WriteNull();
        writer.PopSequence();
        writer.PopSequence();
        writer.PopSequence(ctx1);

        // [2] saltLength
        Asn1Tag ctx2 = new Asn1Tag(TagClass.ContextSpecific, 2, isConstructed: true);
        writer.PushSequence(ctx2);
        writer.WriteInteger(32);
        writer.PopSequence(ctx2);

        writer.PopSequence(); // koniec RSASSA-PSS-params
        writer.PopSequence(); // koniec AlgorithmIdentifier
    }

    /// <summary>
    /// Zapisuje SubjectPublicKeyInfo dla RSA.
    /// </summary>
    private static void WriteRsaPublicKeyInfo(AsnWriter writer, RSA rsa)
    {
        RSAParameters p = rsa.ExportParameters(false);

        // Buduj RSAPublicKey (wewnętrzna zawartość BIT STRING)
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
    /// Zapisuje SubjectPublicKeyInfo dla ECDsa.
    /// OID krzywej jest wyznaczany dynamicznie z parametrów klucza (defense-in-depth).
    /// </summary>
    private static void WriteEcdsaPublicKeyInfo(AsnWriter writer, ECDsa ecdsa)
    {
        ECParameters p = ecdsa.ExportParameters(false);
        string curveOid = EcdsaCompat.CurveToOid(p.Curve);
        int coordLen = p.Q.X!.Length;

        // Punkt nieskompresowany: 0x04 || X || Y
        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(p.Q.X!, 0, point, 1, coordLen);
        Buffer.BlockCopy(p.Q.Y!, 0, point, 1 + coordLen, coordLen);

        // SubjectPublicKeyInfo
        writer.PushSequence();

        // algorithm AlgorithmIdentifier { id-ecPublicKey, namedCurve OID }
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // subjectPublicKey BIT STRING
        writer.WriteBitString(point);

        writer.PopSequence();
    }

    /// <summary>
    /// Koduje ciąg DN oddzielony przecinkami jako ASN.1 DER Name (RDNSequence).
    /// Obsługuje format OID=Wartość (np. "2.5.4.3=Test, 2.5.4.6=PL").
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
    /// Konwertuje podpis ECDSA z formatu IEEE P1363 (r||s) do formatu DER (SEQUENCE { INTEGER r, INTEGER s }).
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
