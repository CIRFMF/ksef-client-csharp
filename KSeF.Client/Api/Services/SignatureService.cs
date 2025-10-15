using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.Api.Services;

/// <inheritdoc />
public class SignatureService : ISignatureService
{
    private const string EcdsaSha256AlgorithmUrl = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
    private const string XadesNsUrl = "http://uri.etsi.org/01903/v1.3.2#";
    private const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";

    /// <inheritdoc />
    /// <summary>
    /// Podpisuje dokument XML przekazany jako ciąg znaków
    /// </summary>
    public string Sign(string xml, X509Certificate2 certificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        ArgumentNullException.ThrowIfNull(certificate);

        var xmlDocument = new XmlDocument() { PreserveWhitespace = true };
        xmlDocument.LoadXml(xml);

        Sign(xmlDocument, certificate);

        return xmlDocument.OuterXml;
    }

    /// <summary>
    /// Podpisuje dokument XML i zwraca podpisany dokument
    /// </summary>
    public XmlDocument Sign(XmlDocument xmlDocument, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        ArgumentNullException.ThrowIfNull(certificate);

        if (xmlDocument.DocumentElement == null)
            throw new ArgumentException("Dokument XML nie ma elementu głównego", nameof(xmlDocument));

        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException("Certyfikat nie zawiera klucza prywatnego");

        RSA rsaKey = certificate.GetRSAPrivateKey();
        ECDsa ecdsaKey = certificate.GetECDsaPrivateKey();

        if (rsaKey == null && ecdsaKey == null)
            throw new InvalidOperationException("Nie można wyodrębnić klucza prywatnego");

        var signatureId = "Signature";
        var signedPropertiesId = "SignedProperties";

        var signedXml = new SignedXmlFixed(xmlDocument);

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

        var qualifyingProperties = BuildQualifyingProperties(
             signatureId, signedPropertiesId,
             certificate, DateTimeOffset.UtcNow.AddMinutes(-1));

        var dataObject = new DataObject { Data = qualifyingProperties };

        signedXml.AddDataObject(dataObject);
        signedXml.ComputeSignature();
        var xmlSignature = signedXml.GetXml();

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
        var rootReference = new Reference(string.Empty);
        rootReference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        rootReference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(rootReference);
    }

    private static void AddSignedPropertiesReference(SignedXml signedXml, string id)
    {
        var xadesReference = new Reference("#" + id)
        {
            Type = SignedPropertiesType
        };

        xadesReference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(xadesReference);
    }

    private static XmlNodeList BuildQualifyingProperties(string signatureId, string signedPropertiesId,
        X509Certificate2 signingCertificate, DateTimeOffset signingTime)
    {
        var certificateDigest = Convert.ToBase64String(signingCertificate.GetCertHash(HashAlgorithmName.SHA256));
        var certificateIssuerName = signingCertificate.Issuer;
        var certificateSerialNumber = new BigInteger(signingCertificate.GetSerialNumber()).ToString();

        var document = new XmlDocument();
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

    [RequiresUnreferencedCode("SignedXml uses algorithm implementations referenced in the XML payload. Ensure required algorithms are preserved.")]
    [RequiresDynamicCode("SignedXml uses XmlDsigXsltTransform which requires dynamic code.")]
    class SignedXmlFixed(XmlDocument document) : SignedXml(document)
    {
        private readonly List<DataObject> _dataObjects = [];

        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            var element = base.GetIdElement(document, idValue);

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
