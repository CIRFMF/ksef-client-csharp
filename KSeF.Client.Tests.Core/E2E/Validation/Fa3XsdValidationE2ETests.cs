using System.Xml;
using System.Xml.Schema;

namespace KSeF.Client.Tests.Core.E2E.Validation;

public class Fa3XsdValidationE2ETests
{
    private const string Fa3Namespace = "http://crd.gov.pl/wzor/2025/06/25/13775/";

        public static TheoryData<string, string, string, string> InvalidInvoiceScenarios => new()
    {
        // 1. Brak wymaganego pola
        {
            "Missing Required Field (KodWaluty)",
            "<KodWaluty>PLN</KodWaluty>",
            "",
            "KodWaluty"
        },

        // 2. Puste wymagane pole
        {
            "Empty Required Field (P_2)",
            "<P_2>FA/GRQMB-12345/05/2025</P_2>",
            "<P_2></P_2>",
            "MinLength"
        },

        // 3. Niewłaściwy typ danych
        {
            "Wrong Data Type (P_9A)",
            "<P_9A>0.90</P_9A>",
            "<P_9A>TEXT</P_9A>",
            "TEXT"
        },

        // 4. Zbyt długi ciąg znaków
        {
            "String Too Long (P_7)",
            "<P_7>Razem za energię czynną</P_7>",
            $"<P_7>{new string('A', 513)}</P_7>",
            "MaxLength"
        },

        // 5. Niepoprawny format daty
        {
            "Invalid Date Format (P_1)",
            "<P_1>2025-12-08</P_1>",
            "<P_1>2025/12/08</P_1>",
            "2025/12/08"
        },

        // 6. Znak niepoprawny w standardzie XML
        {
            "Invalid XML Character (&)",
            "<P_7>Razem za energię czynną</P_7>",
            "<P_7>Tom & Jerry</P_7>",
            "EntityName"
        },

        // 7. Zbyt wiele znaków po przecinku
        {
            "Too Many Decimal Places (P_11 expects max 2)",
            "<P_11>18.00</P_11>",
            "<P_11>18.005</P_11>",
            "18.005"
            },

        // 8. Niepoprawnie zapisana wartość podatku
        {
            "Invalid Tax Rate Enum (P_12)",
            "<P_12>23</P_12>",
            "<P_12>23%</P_12>",
            "23%"
        },

        // 9. Niepoprawnie zapisana wartość logiczna
        {
            "Invalid Boolean Indicator (P_16)",
            "<P_16>2</P_16>",
            "<P_16>false</P_16>",
            "false"
        },

        // 10. Niepoprawnie zapisany kod walutowy
        {
            "Lowercase Currency Code (KodWaluty)",
            "<KodWaluty>PLN</KodWaluty>",
            "<KodWaluty>pln</KodWaluty>",
            "pln"
        },

        // 11. Niepoprawnie zapisany NIP
        {
            "Dashed NIP (NIP)",
            "<NIP>1111111111</NIP>",
            "<NIP>111-111-11-11</NIP>",
            "111-111-11-11"
        }
    };

    [Fact]
    public void Invoice_WithDiacriticsAndEmojis_ShouldPassXsdValidation()
    {
        // 1. Arrange: Wczytanie poprawnej faktury FA(3) i schematu XSD
        string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "invoice-template-fa-3.xml");
        string xsdPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "schemat.xsd");

        string xmlContent = File.ReadAllText(templatePath);

        // Nadpisanie danych zastępczych poprawnymi wartościami, aby weryfikacja XSD nie zatrzymała się na samym początku
        xmlContent = xmlContent.Replace("#nip#", "1234567890");
        xmlContent = xmlContent.Replace("#invoice_number#", "12345");

        // 2. Act: Umieszczenie polskich znaków diakrytycznych oraz emoji w polu tekstowym
        string modifiedXml = xmlContent.Replace(
            "<P_7>Razem za energię czynną</P_7>",
            "<P_7>Zażółć gęślą jaźń 🪿</P_7>");

        // 3. Assert: Record.Exception powinna zwrócić "null", ponieważ XSD stosuje cały zakres UTF-8
        Exception capturedException = Record.Exception(() => ValidateXmlAgainstXsd(modifiedXml, xsdPath));

        Assert.Null(capturedException); // Potwierdza, że XSD wspiera znaki specjalne
    }

    [Theory]
    [MemberData(nameof(InvalidInvoiceScenarios))]
    public void Invoice_WhenInvalid_ShouldFailFa3XsdValidation(
        string scenarioName,
        string targetToReplace,
        string replacementValue,
        string expectedError)
    {
        // 1. Arrange: Wczytanie poprawnej faktury FA(3) i schematu XSD
        string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "invoice-template-fa-3.xml");
        string xsdPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "schemat.xsd");

        string xmlContent = File.ReadAllText(templatePath);

        // Nadpisanie danych zastępczych poprawnymi wartościami, aby weryfikacja XSD nie zatrzymała się na samym początku
        xmlContent = xmlContent.Replace("#nip#", "1234567890");
        xmlContent = xmlContent.Replace("#invoice_number#", "12345");

        // Celowe użycie niepoprawnych danych, w zależności od danego pola
        string badXmlContent = xmlContent.Replace(targetToReplace, replacementValue);

        // 2. Act: Próba walidacji faktury
        Exception capturedException = Record.Exception(() => ValidateXmlAgainstXsd(badXmlContent, xsdPath));

        // 3. Assert: Potwierdzenie wystąpienia błędu
        Assert.True(capturedException != null, $"Scenario '{scenarioName}' failed. Expected an XSD validation error, but the XML was accepted as valid.");
        Assert.Contains(expectedError, capturedException.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Metoda pomocnicza do przeprowadzenia walidacji poprzez porównanie schematu XSD z FA(3)
    /// </summary>
    private static void ValidateXmlAgainstXsd(string xmlContent, string xsdPath)
    {
        XmlSchemaSet schemaSet = new()
        {
            XmlResolver = new XmlUrlResolver()
        };
        schemaSet.Add(Fa3Namespace, xsdPath);

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas = schemaSet;

        settings.ValidationEventHandler += (sender, args) =>
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                throw new XmlSchemaValidationException(args.Message);
            }
        };

        using StringReader stringReader = new StringReader(xmlContent);
        using XmlReader xmlReader = XmlReader.Create(stringReader, settings);

        while (xmlReader.Read()) { }
    }
}
