using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class SignatureService
{
    private const string EcdsaSha256AlgorithmUrl = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
    private const string XadesNsUrl = "http://uri.etsi.org/01903/v1.3.2#";
    private const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";
    private static readonly TimeSpan CertificateTimeBuffer = TimeSpan.FromMinutes(-1);

    /// <summary>
    /// Podpisuje wskazany dokument XML w formacie XAdES, 
    /// używając dostarczonego certyfikatu z kluczem prywatnym.
    /// </summary>
    /// <param name="xml">
    /// Dokument XML (AuthTokenRequest) w formie tekstowej.
    /// </param>
    /// <param name="certificate">
    /// Certyfikat X.509 zawierający klucz prywatny, 
    /// którym ma zostać złożony podpis.
    /// </param>
    /// <returns>
    /// Dokument XML podpisany w formacie XAdES (string).
    /// </returns>
    public static string Sign(string xml, X509Certificate2 certificate)
    {
        Guard.ThrowIfNullOrWhiteSpace(xml);
        Guard.ThrowIfNull(certificate);

        XmlDocument xmlDocument = new() { PreserveWhitespace = true };
        xmlDocument.LoadXml(xml);

        Sign(xmlDocument, certificate);

        return xmlDocument.OuterXml;
    }

    /// <summary>
    /// Podpisuje wskazany dokument XML w formacie XAdES, 
    /// używając dostarczonego certyfikatu z kluczem prywatnym.
    /// Metoda modyfikuje przekazany dokument w miejscu, dodając element podpisu.
    /// </summary>
    /// <param name="xmlDocument">
    /// Dokument XML do podpisania. Musi posiadać element główny.
    /// Zalecane jest ustawienie PreserveWhitespace = true.
    /// </param>
    /// <param name="certificate">
    /// Certyfikat X.509 zawierający klucz prywatny, 
    /// którym ma zostać złożony podpis.
    /// </param>
    /// <returns>
    /// Ten sam obiekt XmlDocument co przekazany w parametrze, 
    /// zmodyfikowany przez dodanie elementu podpisu XAdES.
    /// </returns>
    public static XmlDocument Sign(XmlDocument xmlDocument, X509Certificate2 certificate)
    {
        Guard.ThrowIfNull(xmlDocument);
        Guard.ThrowIfNull(certificate);

        if (xmlDocument.DocumentElement == null)
        {
            throw new ArgumentException("Dokument XML nie ma elementu głównego", nameof(xmlDocument));
        }

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("Certyfikat nie zawiera klucza prywatnego");
        }

        RSA rsaKey = certificate.GetRSAPrivateKey();
        ECDsa ecdsaKey = certificate.GetECDsaPrivateKey();

        if (rsaKey == null && ecdsaKey == null)
        {
            throw new InvalidOperationException("Nie można wyodrębnić klucza prywatnego");
        }

        string signatureId = "Signature";
        string signedPropertiesId = "SignedProperties";

        SignedXmlFixed signedXml = new(xmlDocument);

        if (rsaKey != null)
        {
            signedXml.SigningKey = rsaKey;
        }
        else if (ecdsaKey != null)
        {
            signedXml.SigningKey = ecdsaKey;
            signedXml.SignedInfo.SignatureMethod = EcdsaSha256AlgorithmUrl;
        }

        signedXml.Signature.Id = signatureId;

        AddKeyInfo(signedXml, certificate);
        AddRootReference(signedXml);
        AddSignedPropertiesReference(signedXml, signedPropertiesId);

        XmlNodeList qualifyingProperties = BuildQualifyingProperties(
             signatureId, signedPropertiesId,
             certificate, DateTimeOffset.UtcNow.Add(CertificateTimeBuffer));

        DataObject dataObject = new() { Data = qualifyingProperties };

        signedXml.AddDataObject(dataObject);
        signedXml.ComputeSignature();
        XmlElement xmlSignature = signedXml.GetXml();

        xmlDocument.DocumentElement.AppendChild(xmlDocument.ImportNode(xmlSignature, true));

        return xmlDocument;
    }

    private static void AddKeyInfo(SignedXml signedXml, X509Certificate2 certificate)
    {
        signedXml.KeyInfo = new KeyInfo();
        signedXml.KeyInfo.AddClause(new KeyInfoX509Data(certificate));
    }

    private static void AddRootReference(SignedXml signedXml)
    {
        Reference rootReference = new(string.Empty);
        rootReference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        rootReference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(rootReference);
    }

    private static void AddSignedPropertiesReference(SignedXml signedXml, string id)
    {
        Reference xadesReference = new("#" + id)
        {
            Type = SignedPropertiesType
        };

        xadesReference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(xadesReference);
    }

    private static XmlNodeList BuildQualifyingProperties(string signatureId, string signedPropertiesId,
        X509Certificate2 signingCertificate, DateTimeOffset signingTime)
    {
#if NETSTANDARD2_0
        string certificateDigest = Convert.ToBase64String(HashCompat.SHA256HashData(signingCertificate.RawData));
#else
        string certificateDigest = Convert.ToBase64String(signingCertificate.GetCertHash(HashAlgorithmName.SHA256));
#endif
        string certificateIssuerName = signingCertificate.Issuer;
        string certificateSerialNumber = new BigInteger(signingCertificate.GetSerialNumber()).ToString(CultureInfo.InvariantCulture);

        XmlDocument document = new();
        document.LoadXml(
        $"""
        <xades:QualifyingProperties Target="#{signatureId}" xmlns:xades="{XadesNsUrl}" xmlns="{SignedXml.XmlDsigNamespaceUrl}">
          <xades:SignedProperties Id="{signedPropertiesId}">
            <xades:SignedSignatureProperties>
              <xades:SigningTime>{signingTime:O}</xades:SigningTime>
              <xades:SigningCertificate>
                <xades:Cert>
                  <xades:CertDigest>
                    <DigestMethod Algorithm="{SignedXml.XmlDsigSHA256Url}" />
                    <DigestValue>{certificateDigest}</DigestValue>
                  </xades:CertDigest>
                  <xades:IssuerSerial>
                    <X509IssuerName>{certificateIssuerName}</X509IssuerName>
                    <X509SerialNumber>{certificateSerialNumber}</X509SerialNumber>
                  </xades:IssuerSerial>
                </xades:Cert>
              </xades:SigningCertificate>
            </xades:SignedSignatureProperties>
          </xades:SignedProperties>
        </xades:QualifyingProperties>
        """);

        return document.ChildNodes;
    }

    private class SignedXmlFixed(XmlDocument document) : SignedXml(document)
    {
        private readonly List<DataObject> _dataObjects = [];

        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            XmlElement element = base.GetIdElement(document, idValue);

            return element ?? _dataObjects
                .SelectMany(x => x.Data.Cast<XmlNode>())
                .Select(x => x.SelectSingleNode($"//*[@Id='{idValue}']") as XmlElement)
                .FirstOrDefault(x => x != null);
        }

        public void AddDataObject(DataObject dataObject)
        {
            _dataObjects.Add(dataObject);
            AddObject(dataObject);
        }
    }
}
