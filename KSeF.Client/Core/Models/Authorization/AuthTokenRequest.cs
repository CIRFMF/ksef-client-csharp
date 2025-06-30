using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace KSeF.Client.Core.Models.Authorization;

[XmlRoot("AuthTokenRequest", Namespace = "http://ksef.mf.gov.pl/auth/token/2.0")]
public class AuthTokenRequest
{
    public string Challenge { get; set; }

    public AuthContextIdentifier ContextIdentifier { get; set; }

    public SubjectIdentifierTypeEnum SubjectIdentifierType { get; set; }

    public IpAddressPolicy IpAddressPolicy { get; set; }    
}

public class AuthContextIdentifier
{
    public ContextIdentifierType Type { get; set; }
    public string Value { get; set; }
}
public enum ContextIdentifierType
{
    [EnumMember(Value = "nip")]
    [XmlEnum("nip")]
    Nip,
    [EnumMember(Value = "internalId")]
    [XmlEnum("internalId")]
    InternalId,
    [EnumMember(Value = "nipVatUe")]
    [XmlEnum("nipVatUe")]
    NipVatUe
}

public enum SubjectIdentifierTypeEnum
{
    [EnumMember(Value = "certificateSubject")]
    [XmlEnum("certificateSubject")]
    CertificateSubject,
    [EnumMember(Value = "certificateFingerprint ")]
    [XmlEnum("certificateFingerprint ")]
    CertificateFingerprint
}

public class IpAddressPolicy
{
    public IpChangePolicy OnClientIpChange { get; set; }

    public AllowedIps AllowedIps { get; set; }
}

public enum IpChangePolicy
{
    [XmlEnum("reject")]
    [EnumMember(Value = "reject")]
    Reject,

    [XmlEnum("ignore")]
    [EnumMember(Value = "ignore")]
    Ignore
}

public class AllowedIps
{
    [XmlElement("IpAddress")]
    public List<string> IpAddress { get; set; } = new();

    [XmlElement("IpRange")]
    public List<string> IpRange { get; set; } = new();

    [XmlElement("IpMask")]
    public List<string> IpMask { get; set; } = new();
}

public class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}

public static class AuthTokenRequestSerializer
{
    public static string SerializeToXmlString(this AuthTokenRequest request)
    {
        var serializer = new XmlSerializer(typeof(AuthTokenRequest));

        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };

        using var sw = new Utf8StringWriter();
        using var writer = XmlWriter.Create(sw, settings);
        serializer.Serialize(writer, request);
        return sw.ToString();
    }
}
