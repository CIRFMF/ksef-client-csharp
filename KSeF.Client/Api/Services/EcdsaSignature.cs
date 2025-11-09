using System.Security.Cryptography;

namespace KSeF.Client.Api.Services;

public class Ecdsa256SignatureDescription : SignatureDescription
{
    public Ecdsa256SignatureDescription()
    {
        KeyAlgorithm = typeof(ECDsa).AssemblyQualifiedName;
    }
        
    public override HashAlgorithm CreateDigest() => SHA256.Create();

    public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
    {
        if (!(key is ECDsa ecdsa) || ecdsa.KeySize != 256) throw new InvalidOperationException("Requires EC key using P-256");
        return new EcdsaSignatureFormatter(ecdsa);
    }

    public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
    {
        if (!(key is ECDsa ecdsa) || ecdsa.KeySize != 256) throw new InvalidOperationException("Requires EC key using P-256");
        return new EcdsaSignatureDeformatter(ecdsa);
    }
}

public class EcdsaSignatureFormatter : AsymmetricSignatureFormatter
{
    private ECDsa key;

    public EcdsaSignatureFormatter(ECDsa key) => this.key = key;

    public override void SetKey(AsymmetricAlgorithm key) => this.key = key as ECDsa;
        
    public override void SetHashAlgorithm(string strName) { }

    public override byte[] CreateSignature(byte[] rgbHash) => key.SignHash(rgbHash);
}

public class EcdsaSignatureDeformatter : AsymmetricSignatureDeformatter
{
    private ECDsa key;

    public EcdsaSignatureDeformatter(ECDsa key) => this.key = key;

    public override void SetKey(AsymmetricAlgorithm key) => this.key = key as ECDsa;
        
    public override void SetHashAlgorithm(string strName) { }

    public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => key.VerifyHash(rgbHash, rgbSignature);
}
