#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <c>CertificateRequest.CreateSigningRequest()</c>
/// which is not available on netstandard2.0 / .NET Framework 4.8.
/// Builds a PKCS#10 CertificationRequest using raw ASN.1 encoding (RFC 2986).
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
    /// Creates a PKCS#10 CSR with an RSA key signed using RSA-PSS with SHA-256.
    /// </summary>
    /// <param name="subjectDerBytes">DER-encoded X.500 Name (subject).</param>
    /// <param name="rsa">The RSA key pair.</param>
    /// <param name="padding">RSA signature padding (PSS or PKCS#1).</param>
    /// <returns>DER-encoded PKCS#10 CertificationRequest.</returns>
    public static byte[] CreateSigningRequestRsa(byte[] subjectDerBytes, RSA rsa, RSASignaturePadding padding)
    {
        bool usePss = padding == RSASignaturePadding.Pss;
        byte[] certRequestInfo = BuildCertificationRequestInfo(subjectDerBytes, rsa, isEcdsa: false);

        // Sign with RSACng if PSS is needed
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
    /// Creates a PKCS#10 CSR with an ECDsa key signed using ECDSA with SHA-256.
    /// </summary>
    /// <param name="subjectDerBytes">DER-encoded X.500 Name (subject).</param>
    /// <param name="ecdsa">The ECDsa key pair.</param>
    /// <returns>DER-encoded PKCS#10 CertificationRequest.</returns>
    public static byte[] CreateSigningRequestEcdsa(byte[] subjectDerBytes, ECDsa ecdsa)
    {
        byte[] certRequestInfo = BuildCertificationRequestInfo(subjectDerBytes, ecdsa, isEcdsa: true);
        // ECDsa.SignData on .NET Framework returns IEEE P1363 format (r||s).
        // PKCS#10 CSR requires DER-encoded ECDSA signature (SEQUENCE { INTEGER r, INTEGER s }).
        byte[] ieeeSignature = ecdsa.SignData(certRequestInfo, HashAlgorithmName.SHA256);
        byte[] derSignature = ConvertIeeeP1363ToDer(ieeeSignature);
        return WrapCsr(certRequestInfo, derSignature, isEcdsa: true, usePss: false);
    }

    /// <summary>
    /// Builds the CertificationRequestInfo ASN.1 structure (RFC 2986 §4.1).
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

        // subject Name (already DER-encoded)
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

        // attributes [0] IMPLICIT SET OF Attribute (empty)
        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSetOf(ctx0);
        writer.PopSetOf(ctx0);

        writer.PopSequence();
        return writer.Encode();
    }

    /// <summary>
    /// Wraps CertificationRequestInfo + signature into the final CertificationRequest.
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

    private static void WriteEcdsaPublicKeyInfo(AsnWriter writer, ECDsa ecdsa)
    {
        ECParameters p = ecdsa.ExportParameters(false);
        int coordLen = p.Q.X.Length;

        byte[] point = new byte[1 + coordLen * 2];
        point[0] = 0x04;
        Buffer.BlockCopy(p.Q.X, 0, point, 1, coordLen);
        Buffer.BlockCopy(p.Q.Y, 0, point, 1 + coordLen, coordLen);

        writer.PushSequence();
        writer.PushSequence();
        writer.WriteObjectIdentifier(EcPublicKeyOid);
        writer.WriteObjectIdentifier(NistP256Oid);
        writer.PopSequence();
        writer.WriteBitString(point);
        writer.PopSequence();
    }

    private static void WriteRsaPssAlgorithmIdentifier(AsnWriter writer)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaSsaPssOid);

        writer.PushSequence();

        Asn1Tag ctx0 = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSequence(ctx0);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
        writer.PopSequence();
        writer.PopSequence(ctx0);

        Asn1Tag ctx1 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
        writer.PushSequence(ctx1);
        writer.PushSequence();
        writer.WriteObjectIdentifier(MgfSha256Oid);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256Oid);
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
