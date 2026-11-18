#nullable enable
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Http;

namespace KSeF.Client.Tests;

/// <summary>
/// Testy zgodności z zasadami wersjonowania i kompatybilności API KSeF
/// (CIRFMF/ksef-api#866 – „Wersjonowanie i zgodność API").
/// </summary>
public class ContractCompatibilityPolicyTests
{
    /// <summary>
    /// Przykładowy enum obrazujący rozszerzalną wartość wyliczeniową (kontrakty rewizji operacji).
    /// </summary>
    private enum SampleCode
    {
        Known,
        FutureValue
    }

    /// <summary>
    /// Klient nie może odrzucać odpowiedzi wyłącznie z powodu obecności nieznanych właściwości.
    /// Dotyczy to także właściwości obiektów zagnieżdżonych.
    /// </summary>
    [Fact]
    public void DeserializeResponse_WithUnknownTopLevelAndNestedProperties_IgnoresThem()
    {
        const string json = """
            {
              "value": "5265877635-12345",
              "brandNewTopLevelField": "ignored",
              "type": { "code": "Known", "rawCode": "Known", "displayName": "Znany", "brandNewNestedField": 42 }
            }
            """;

        ExtensibleIdentifier<SampleCode> identifier = JsonUtil.Deserialize<ExtensibleIdentifier<SampleCode>>(json);

        Assert.Equal("5265877635-12345", identifier.Value);
        Assert.Equal(SampleCode.Known, identifier.Type.Code);
    }

    /// <summary>
    /// Wartość zastępcza <c>FutureValue</c> występuje wyłącznie w odpowiedziach – nie może być
    /// wartością typu wyliczeniowego używanego w żądaniu.
    /// </summary>
    [Fact]
    public void RequestEnum_DoesNotContainFutureValue()
    {
        Assert.False(Enum.IsDefined(typeof(KsefTokenPermissionType), "FutureValue"));
    }

    [Fact]
    public void RequestSerialization_NeverEmitsFutureValue()
    {
        KsefTokenRequest request = new()
        {
            Permissions = [KsefTokenPermissionType.InvoiceRead, KsefTokenPermissionType.InvoiceWrite],
            Description = "Opis tokena"
        };

        string json = JsonUtil.Serialize(request);

        Assert.DoesNotContain("FutureValue", json, StringComparison.Ordinal);
        Assert.Contains("InvoiceRead", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Enum odpowiedzi z wartością zastępczą <c>FutureValue</c> jest obsługiwany przez dedykowany konwerter.
    /// </summary>
    [Fact]
    public void ResponseEnum_WithFutureValuePlaceholder_IsSupported()
    {
        Assert.True(Enum.IsDefined(typeof(SampleCode), "FutureValue"));
        Assert.True(new FutureValueEnumConverterFactory().CanConvert(typeof(SampleCode)));
    }

    /// <summary>
    /// Generyczna wartość rozszerzalna przenosi pola <c>code</c>, <c>rawCode</c> i <c>displayName</c>.
    /// </summary>
    [Fact]
    public void ExtensibleEnumValue_DeserializesCodeRawCodeAndDisplayName()
    {
        const string json = """{ "code": "Known", "rawCode": "Known", "displayName": "Znany" }""";

        ExtensibleEnumValue<SampleCode> value = JsonUtil.Deserialize<ExtensibleEnumValue<SampleCode>>(json);

        Assert.Equal(SampleCode.Known, value.Code);
        Assert.Equal("Known", value.RawCode);
        Assert.Equal("Znany", value.DisplayName);
    }

    /// <summary>
    /// Nieznana wartość enuma zwrócona przez API jest mapowana na wartość zastępczą <c>FutureValue</c>,
    /// a jej rzeczywisty kod pozostaje dostępny w <c>rawCode</c>.
    /// </summary>
    [Fact]
    public void DeserializeResponse_WithUnknownEnumCode_MapsToFutureValueAndKeepsRawCode()
    {
        const string json = """{ "code": "SomethingBrandNew", "rawCode": "SomethingBrandNew", "displayName": "Nowa wartość" }""";

        ExtensibleEnumValue<SampleCode> value = JsonUtil.Deserialize<ExtensibleEnumValue<SampleCode>>(json);

        Assert.Equal(SampleCode.FutureValue, value.Code);
        Assert.Equal("SomethingBrandNew", value.RawCode);
        Assert.Equal("Nowa wartość", value.DisplayName);
    }

    /// <summary>
    /// Typy wyliczeniowe odpowiedzi kontraktu Rev2 udostępniają wartość zastępczą <c>FutureValue</c>,
    /// dzięki czemu nowa wartość biznesowa nie zrywa deserializacji starszych rewizji.
    /// </summary>
    [Theory]
    [InlineData(typeof(KsefTokenStatusRev2Code))]
    [InlineData(typeof(KsefTokenPermissionCodeRev2))]
    [InlineData(typeof(KsefTokenAuthorIdentifierTypeCodeRev2))]
    [InlineData(typeof(KsefTokenContextIdentifierTypeCodeRev2))]
    public void ResponseEnums_ExposeFutureValuePlaceholder(Type enumType)
    {
        Assert.True(Enum.IsDefined(enumType, "FutureValue"));
    }

    /// <summary>
    /// Żądanie wygenerowania tokena Rev2 nie emituje wartości zastępczej <c>FutureValue</c>.
    /// </summary>
    [Fact]
    public void GenerateTokenRev2Request_Serialization_NeverEmitsFutureValue()
    {
        KsefTokenRequestRev2 request = new()
        {
            Permissions = [KsefTokenPermissionType.InvoiceRead, KsefTokenPermissionType.InvoiceWrite],
            Description = "Opis tokena"
        };

        string json = JsonUtil.Serialize(request);

        Assert.DoesNotContain("FutureValue", json, StringComparison.Ordinal);
        Assert.Contains("InvoiceRead", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Model odpowiedzi Rev2 ignoruje nieznane właściwości (także zagnieżdżone).
    /// </summary>
    [Fact]
    public void DeserializeRev2Token_WithUnknownProperties_IgnoresThem()
    {
        const string json = """
            {
              "referenceNumber": "20251001-EC-220B0CE000-E228129563-96",
              "description": "Opis tokena",
              "dateCreated": "2026-11-10T12:23:56.0154302+00:00",
              "validFrom": "2026-11-10T12:23:56.0154302+00:00",
              "validTo": "2026-11-20T12:23:56.0154302+00:00",
              "brandNewTopLevelField": "ignored",
              "status": { "code": "Active", "rawCode": "Active", "displayName": "Token aktywny", "brandNewStatusField": 42 },
              "authorIdentifier": { "type": { "code": "Nip", "rawCode": "Nip", "displayName": "NIP", "extra": "x" }, "value": "7762811692", "yetAnother": true }
            }
            """;

        AuthenticationKsefTokenRev2 token = JsonUtil.Deserialize<AuthenticationKsefTokenRev2>(json);

        Assert.Equal("20251001-EC-220B0CE000-E228129563-96", token.ReferenceNumber);
        Assert.Equal(KsefTokenStatusRev2Code.Active, token.Status.Code);
        Assert.Equal("7762811692", token.AuthorIdentifier.Value);
    }

    /// <summary>
    /// Generyczny identyfikator kontraktu Rev2 przenosi pola <c>type</c> i <c>value</c>.
    /// </summary>
    [Fact]
    public void ExtensibleIdentifierRev2_DeserializesTypeAndValue()
    {
        const string json = """{ "type": { "code": "InternalId", "rawCode": "InternalId", "displayName": "Identyfikator wewnętrzny" }, "value": "5265877635-12345" }""";

        KsefTokenContextIdentifierRev2 identifier = JsonUtil.Deserialize<KsefTokenContextIdentifierRev2>(json);

        Assert.Equal(KsefTokenContextIdentifierTypeCodeRev2.InternalId, identifier.Type.Code);
        Assert.Equal("5265877635-12345", identifier.Value);
    }
}
