#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla <c>CertificateRequest.CreateSigningRequest()</c>,
/// który nie jest dostępny na netstandard2.0 / .NET Framework 4.8.
/// Buduje żądanie certyfikacji PKCS#10 przy użyciu surowego kodowania ASN.1 (RFC 2986).
/// </summary>
internal static class CsrCompat
{
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";
    private const string RsaSsaPssOid = "1.2.840.113549.1.1.10";
    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
    private const string MgfSha256Oid = "1.2.840.113549.1.1.8";
    private const string EcPublicKeyOid = "1.2.840.10045.2.1";
    private const string EcdsaWithSha256Oid = "1.2.840.10045.4.3.2";
    private const string NistP256Oid = "1.2.840.10045.3.1.7";

    /// <summary>
    /// Tworzy żądanie certyfikacji PKCS#10 (CSR) z kluczem RSA, podpisane przy użyciu RSA-PSS z SHA-256.
    /// </summary>
    /// <param name="subjectDerBytes">Zakodowana w DER nazwa X.500 (podmiot).</param>
    /// <param name="rsa">Para kluczy RSA.</param>
    /// <param name="padding">Wypełnienie podpisu RSA (PSS lub PKCS#1).</param>
    /// <returns>Zakodowane w DER żądanie certyfikacji PKCS#10.</returns>
    public static byte[] CreateSigningRequestRsa(byte[] subjectDerBytes, RSA rsa, RSASignaturePadding padding)
    {
        PlatformGuard.EnsureWindowsCng();
        bool usePss = padding == RSASignaturePadding.Pss;
        byte[] certRequestInfo = BuildCertificationRequestInfo(subjectDerBytes, rsa, isEcdsa: false);

        // Podpisz za pomocą RSACng jeśli wymagany jest PSS
        byte[] signature;
        if (usePss)
        {
            RSAParameters parameters = rsa.ExportParameters(true);
            using (RSACng cng = new RSACng())
            {
                cng.ImportParameters(parameters);
                signature = cng.SignData(certRequestInfo, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
        }
        else
        {
            signature = rsa.SignData(certRequestInfo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        return WrapCsr(certRequestInfo, signature, isEcdsa: false, usePss: usePss);
    }

    /// <summary>
    /// Tworzy żądanie certyfikacji PKCS#10 (CSR) z kluczem ECDsa, podpisane przy użyciu ECDSA z SHA-256.
    /// </summary>
    /// <param name="subjectDerBytes">Zakodowana w DER nazwa X.500 (podmiot).</param>
    /// <param name="ecdsa">Para kluczy ECDsa.</param>
    /// <returns>Zakodowane w DER żądanie certyfikacji PKCS#10.</returns>
    public static byte[] CreateSigningRequestEcdsa(byte[] subjectDerBytes, ECDsa ecdsa)
    {
        PlatformGuard.EnsureWindowsCng();
        byte[] certRequestInfo = BuildCertificationRequestInfo(subjectDerBytes, ecdsa, isEcdsa: true);
        // ECDsa.SignData na .NET Framework zwraca format IEEE P1363 (r||s).
        // PKCS#10 CSR wymaga podpisu ECDSA zakodowanego w DER (SEQUENCE { INTEGER r, INTEGER s }).
        byte[] ieeeSignature = ecdsa.SignData(certRequestInfo, HashAlgorithmName.SHA256);
        byte[] derSignature = ConvertIeeeP1363ToDer(ieeeSignature);
        return WrapCsr(certRequestInfo, derSignature, isEcdsa: true, usePss: false);
    }

    /// <summary>
    /// Buduje strukturę ASN.1 CertificationRequestInfo (RFC 2986 §4.1).
    /// <code>
    /// CertificationRequestInfo ::= SEQUENCE {
    ///     version       INTEGER { v1(0) },
    ///     subject       Name,
    ///     subjectPKInfo SubjectPublicKeyInfo,
    ///     attributes    [0] Attributes
    /// }
    /// </code>
    /// </summary>
    private static byte[] BuildCertificationRequestInfo(byte[] subjectDerBytes, AsymmetricAlgorithm key, bool isEcdsa)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // version INTEGER (0 = v1)
        writer.WriteInteger(0);

        // subject Name (już zakodowane w DER)
        writer.WriteEncodedValue(subjectDerBytes);

        // subjectPKInfo SubjectPublicKeyInfo
        if (isEcdsa)
        {
            WriteEcdsaPublicKeyInfo(writer, (ECDsa)key);
        }
        else
        {
            WriteRsaPublicKeyInfo(writer, (RSA)key);
        }

        // attributes [0] IMPLICIT SET OF Attribute (pusty)
        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSetOf(ctx0);
        writer.PopSetOf(ctx0);

        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Opakowuje CertificationRequestInfo + podpis w finalną strukturę CertificationRequest.
    /// </summary>
    private static byte[] WrapCsr(byte[] certRequestInfo, byte[] signature, bool isEcdsa, bool usePss)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // certificationRequestInfo
        writer.WriteEncodedValue(certRequestInfo);

        // signatureAlgorithm
        if (isEcdsa)
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier(EcdsaWithSha256Oid);
            writer.PopSequence();
        }
        else if (usePss)
        {
            WriteRsaPssAlgorithmIdentifier(writer);
        }
        else
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier("1.2.840.113549.1.1.11"); // sha256WithRSAEncryption
            writer.WriteNull();
            writer.PopSequence();
        }

        // signature BIT STRING
        writer.WriteBitString(signature);

        writer.PopSequence();
        return writer.Encode();
    }

    private static void WriteRsaPublicKeyInfo(AsnWriter writer, RSA rsa)
    {
        RSAParameters p = rsa.ExportParameters(false);

        AsnWriter pubKeyWriter = new AsnWriter(AsnEncodingRules.DER);
        pubKeyWriter.PushSequence();
        pubKeyWriter.WriteIntegerUnsigned(p.Modulus);
        pubKeyWriter.WriteIntegerUnsigned(p.Exponent);
        pubKeyWriter.PopSequence();
        byte[] rsaPubKey = pubKeyWriter.Encode();

        writer.PushSequence();
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();
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
        int coordLen = p.Q.X.Length;

        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(p.Q.X, 0, point, 1, coordLen);
        Buffer.BlockCopy(p.Q.Y, 0, point, 1 + coordLen, coordLen);

        writer.PushSequence();
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();
        writer.WriteBitString(point);
        writer.PopSequence();
    }

    /// <summary>
    /// Zapisuje AlgorithmIdentifier RSASSA-PSS z parametrami SHA-256 (RFC 4055 §2.1).
    /// </summary>
    /// <remarks>
    /// Per RFC 4055 §2.1: AlgorithmIdentifier dla SHA-256 wewnątrz RSASSA-PSS-params
    /// MUSI zawierać NULL jako parametr algorytmu haszowania.
    /// </remarks>
    private static void WriteRsaPssAlgorithmIdentifier(AsnWriter writer)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaSsaPssOid);

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

        Asn1Tag ctx2 = new Asn1Tag(TagClass.ContextSpecific, 2, isConstructed: true);
        writer.PushSequence(ctx2);
        writer.WriteInteger(32);
        writer.PopSequence(ctx2);

        writer.PopSequence();
        writer.PopSequence();
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
