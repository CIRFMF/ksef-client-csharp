#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.Security.Cryptography;

namespace KSeF.Client.Extensions;

/// <summary>
/// Opis podpisu ECDSA z SHA-256 dla <see cref="System.Security.Cryptography.Xml.SignedXml"/>.
/// Umożliwia używanie kluczy ECDSA P-256 do podpisów XAdES.
/// </summary>
public class Ecdsa256SignatureDescription : SignatureDescription
{
    public Ecdsa256SignatureDescription()
    {
        KeyAlgorithm = typeof(ECDsa).AssemblyQualifiedName;
    }

#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
#endif
    public override HashAlgorithm CreateDigest() => SHA256.Create();

#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
#endif
    public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
    {
        if (key is not ECDsa ecdsa)
        {
            throw new InvalidOperationException("Wymagany klucz ECDSA");
        }

        return new ECDsaSignatureFormatter(ecdsa);
    }

#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
#endif
    public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
    {
        if (key is not ECDsa ecdsa)
        {
            throw new InvalidOperationException("Wymagany klucz ECDSA");
        }

        return new ECDsaSignatureDeformatter(ecdsa);
    }
}

/// <summary>
/// Formatter podpisu ECDSA — tworzy podpis cyfrowy z użyciem klucza ECDsa.
/// </summary>
public class ECDsaSignatureFormatter(ECDsa key) : AsymmetricSignatureFormatter
{
    private ECDsa ecdsaKey = key;

    public override void SetKey(AsymmetricAlgorithm key) => ecdsaKey = key as ECDsa;

    public override void SetHashAlgorithm(string strName) { }

    public override byte[] CreateSignature(byte[] rgbHash)
    {
        if (ecdsaKey == null)
        {
            throw new CryptographicException("Brak klucza ECDSA");
        }

        return ecdsaKey.SignHash(rgbHash);
    }
}

/// <summary>
/// Deformatter podpisu ECDSA — weryfikuje podpis cyfrowy z użyciem klucza ECDsa.
/// </summary>
public class ECDsaSignatureDeformatter(ECDsa ecdsaKey) : AsymmetricSignatureDeformatter
{
    public override void SetKey(AsymmetricAlgorithm key) => ecdsaKey = key as ECDsa;

    public override void SetHashAlgorithm(string strName) { }

    public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
    {
        if (ecdsaKey == null)
        {
            throw new CryptographicException("Brak klucza ECDSA");
        }

        return ecdsaKey.VerifyHash(rgbHash, rgbSignature);
    }
}
