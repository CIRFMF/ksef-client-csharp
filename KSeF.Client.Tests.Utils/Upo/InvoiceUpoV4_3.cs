#nullable enable
using System.Xml;
using System.Xml.Serialization;

namespace KSeF.Client.Tests.Utils.Upo;

[XmlRoot("Potwierdzenie", Namespace = "http://upo.schematy.mf.gov.pl/KSeF/v4-3")]
public class InvoiceUpoV4_3 : IUpoParsable
{
	[XmlElement("NazwaPodmiotuPrzyjmujacego")]
	public required string ReceivingEntityName { get; set; }

	[XmlElement("NumerReferencyjnySesji")]
	public required string SessionReferenceNumber { get; set; }

	[XmlElement("Uwierzytelnienie")]
	public required UpoAuthentication Authentication { get; set; }

	[XmlElement("NazwaStrukturyLogicznej")]
	public required string LogicalStructureName { get; set; }

	[XmlElement("KodFormularza")]
	public required string FormCode { get; set; }

	[XmlElement("Dokument")]
	public required Document Document { get; set; }

	[XmlAnyElement("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#")]
	public XmlElement? Signature { get; set; }
}

