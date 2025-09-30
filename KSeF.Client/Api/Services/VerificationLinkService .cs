using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.DI;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Api.Services
{
    public class VerificationLinkService : IVerificationLinkService
    {
        private readonly string BaseUrl;
        
        public VerificationLinkService(KSeFClientOptions options)
        {        
            BaseUrl = $"{options.BaseUrl}/client-app";    
        }

        public string BuildInvoiceVerificationUrl(string nip, DateTime issueDate, string invoiceHash)
        {
            var date = issueDate.ToString("dd-MM-yyyy");
            var bytes = Convert.FromBase64String(invoiceHash);
            var urlEncoded = Base64Url_EncodeToString(bytes);
            return $"{BaseUrl}/invoice/{nip}/{date}/{urlEncoded}";
        }

        public string BuildCertificateVerificationUrl(
            string sellerNip,
            ContextIdentifierType contextIdentifierType,
            string contextIdentifierValue,
            string certificateSerial,
            string invoiceHash,
            X509Certificate2 signingCertificate,
            string privateKey = ""
        )
        {            
            var bytes = Convert.FromBase64String(invoiceHash);
            var invoiceHashUrlEncoded = Base64Url_EncodeToString(bytes);

            var pathToSign = $"{BaseUrl}/certificate/{contextIdentifierType}/{contextIdentifierValue}/{sellerNip}/{certificateSerial}/{invoiceHashUrlEncoded}".Replace("https://", "");
            var signedHash = ComputeUrlEncodedSignedHash(pathToSign, signingCertificate, privateKey);

            return $"{BaseUrl}/certificate/{contextIdentifierType}/{contextIdentifierValue}/{sellerNip}/{certificateSerial}/{invoiceHashUrlEncoded}/{signedHash}";
        }


        private static string ComputeUrlEncodedSignedHash(string pathToSign, X509Certificate2 cert, string privateKey = "", DSASignatureFormat dSASignatureFormat = DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
        {
            // 1. SHA-256
            byte[] sha;

            using (var sha256 = SHA256.Create())
            {
                sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(pathToSign));            
            }

            if (!string.IsNullOrEmpty(privateKey))
            {                
                if (privateKey.StartsWith("-----"))
                {
                    privateKey = string.Concat(
                        privateKey
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(l => !l.StartsWith("-----"))
                    );
                }

                byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

                // 1.1 Importujemy tylko, gdy certyfikat nie ma klucza prywatnego
                if (!cert.HasPrivateKey)
                {
                    if (cert.GetRSAPublicKey() != null)
                    {
                        using var rsaTemp = RSA.Create();
                        rsaTemp.ImportRSAPrivateKey(privateKeyBytes, out _);
                        cert = cert.CopyWithPrivateKey(rsaTemp);
                    }
                    else if (cert.GetECDsaPublicKey() != null)
                    {
                        using var ecdsaTemp = ECDsa.Create();
                        ecdsaTemp.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                        cert = cert.CopyWithPrivateKey(ecdsaTemp);
                    }
                    else
                    {
                        throw new InvalidOperationException("Certyfikat nie wspiera RSA ani ECDSA.");
                    }
                }
            }
            // 2. Sign hash
            byte[] signature;
            if (cert.GetRSAPrivateKey() is RSA rsa)
            {
                signature = rsa.SignHash(sha, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            else if (cert.GetECDsaPrivateKey() is ECDsa ecdsa)
            {
                signature = ecdsa.SignHash(sha, dSASignatureFormat);
            }
            else
            {
                throw new InvalidOperationException("Certyfikat nie wspiera RSA ani ECDsa.");
            }

            // 3. Base64 + URL-encode            
            return Base64Url_EncodeToString(signature);            
        }

		private static string Base64Url_EncodeToString(byte[] blob)
		{
#if NET8_0
			return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#else
			return Base64Url.EncodeToString(blob);
#endif
		}
	}
}
