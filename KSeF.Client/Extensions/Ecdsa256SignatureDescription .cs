using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace KSeF.Client.Extensions;
public class Ecdsa256SignatureDescription : SignatureDescription
{
    public Ecdsa256SignatureDescription()
    {
        KeyAlgorithm = typeof(ECDsa).AssemblyQualifiedName;
    }

    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
    public override HashAlgorithm CreateDigest() => SHA256.Create();
    
    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
    public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
    {
        if (key is not ECDsa ecdsa)
            throw new InvalidOperationException("Wymagany klucz ECDSA");
        return new ECDsaSignatureFormatter(ecdsa);
    }

    [RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
    public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
    {
        if (key is not ECDsa ecdsa)
            throw new InvalidOperationException("Wymagany klucz ECDSA");
        return new ECDsaSignatureDeformatter(ecdsa);
    }
}

public class ECDsaSignatureFormatter : AsymmetricSignatureFormatter
{
    private ECDsa _ecdsaKey;

    public ECDsaSignatureFormatter(ECDsa key) => _ecdsaKey = key;

    public override void SetKey(AsymmetricAlgorithm key) => _ecdsaKey = key as ECDsa;

    public override void SetHashAlgorithm(string strName) { }

    public override byte[] CreateSignature(byte[] rgbHash)
    {
        if (_ecdsaKey == null)
            throw new CryptographicException("Brak klucza ECDSA");
        return _ecdsaKey.SignHash(rgbHash);
    }
}

public class ECDsaSignatureDeformatter : AsymmetricSignatureDeformatter
{
    private ECDsa _ecdsaKey;

    public ECDsaSignatureDeformatter(ECDsa key) => _ecdsaKey = key;

    public override void SetKey(AsymmetricAlgorithm key) => _ecdsaKey = key as ECDsa;

    public override void SetHashAlgorithm(string strName) { }

    public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
    {
        if (_ecdsaKey == null)
            throw new CryptographicException("Brak klucza ECDSA");
        return _ecdsaKey.VerifyHash(rgbHash, rgbSignature);
    }
}
