#nullable enable
using KSeF.Client.Validation;

namespace KSeF.Client.Tests.Core.UnitTests;

public class TypeValueValidatorTests
{
    public enum ValidationType
    {
        Nip,
        Pesel,
        NipVatUe,
        InternalId,
        Fingerprint,
        PeppolId,
        AllPartners,
        NoMatchingRule,
    }

    /// <summary>
    /// Obiekt o polach Type i Value do celów testowych.
    /// </summary>
    public class ValidatableObject
    {
        public ValidationType? Type { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// Obiekt bez pola Value do celów testowych.
    /// </summary>
    public class ObjectWithoutValue
    {
        public ValidationType Type { get; set; }
    }

    /// <summary>
    /// Obiekt bez pola Type do celów testowych.
    /// </summary>
    public class ObjectWithoutType
    {
        public string? Value { get; set; }
    }

    /// <summary>
    /// Obiekt z polem Type o niewłaściwym typie do celów testowych.
    /// </summary>
    public class ObjectWithWrongTypeProperty
    {
        public int Type { get; set; }
        public string? Value { get; set; }
    }

    [Theory]
    [InlineData("9876543210", true)]  // Poprawny NIP
    [InlineData("12345", false)]       // Niepoprawny NIP
    [InlineData("not-a-nip", false)]  // Niepoprawny NIP
    [InlineData("123-456-78-90", false)]  // Niepoprawny NIP
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Validate_WithNipType_ReturnsExpectedResult(string? nipValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new () { Type = ValidationType.Nip, Value = nipValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("99010112345", true)] // Poprawny PESEL
    [InlineData("9901011234", false)]  // Za krótki PESEL
    [InlineData("990101123411", false)]  // Za długi PESEL
    [InlineData("asdfghjklz", false)]  // 10-znakowy nie-PESEL
    [InlineData(null, false)]  
    [InlineData("", false)]
    public void Validate_WithPeselType_ReturnsExpectedResult(string? peselValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new () { Type = ValidationType.Pesel, Value = peselValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("9876543210-DE123456789", true)] // Poprawny NIP z niemieckim VAT UE
    [InlineData("9876543210-PL", false)]         // Niepoprawny format VAT UE (PL nie występuje w regexie)
    [InlineData("9876543210DE123456789", false)] // Brak myślnika
    [InlineData("9876543210-", false)]           // Tylko NIP z myślnikiem
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Validate_WithNipVatUeType_ReturnsExpectedResult(string? nipVatUeValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new() { Type = ValidationType.NipVatUe, Value = nipVatUeValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("9876543210-12345", true)]  // Poprawny InternalId
    [InlineData("987654321012345", false)]  // Brak myślnika
    [InlineData("9876543210-1234", false)]  // Za krótka druga część
    [InlineData("987654321-12345", false)]  // Za krótka pierwsza część
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Validate_WithInternalIdType_ReturnsExpectedResult(string? internalIdValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new() { Type = ValidationType.InternalId, Value = internalIdValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2", true)] // Poprawny fingerprint (64 znaki)
    [InlineData("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2", false)] // Małe litery są niedozwolone
    [InlineData("A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B", false)]  // Za krótki (63 znaki)
    [InlineData("A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B23", false)]// Za długi (65 znaków)
    [InlineData("A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1BG", false)] // Zawiera nie-heksadecymalny znak 'G'
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Validate_WithFingerprintType_ReturnsExpectedResult(string? fingerprintValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new() { Type = ValidationType.Fingerprint, Value = fingerprintValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("PPL123456", true)]  // Poprawny PeppolId
    [InlineData("APL123456", false)] // Nie zaczyna się od 'P'
    [InlineData("PPL12345", false)]  // Za mało cyfr
    [InlineData("PPL1234567", false)]// Za dużo cyfr
    [InlineData("PPLA12345", false)] // Litera w części numerycznej
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Validate_WithPeppolIdType_ReturnsExpectedResult(string? peppolIdValue, bool expectedResult)
    {
        // Arrange
        ValidatableObject objectUnderTest = new() { Type = ValidationType.PeppolId, Value = peppolIdValue };

        // Act
        bool result = TypeValueValidator.Validate(objectUnderTest);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ForTypeAllPartnersAndEmptyValue_ReturnsTrue(string? value)
    {
        // Arrange
        ValidatableObject obj = new() { Type = ValidationType.AllPartners, Value = value };

        // Act
        bool result = TypeValueValidator.Validate(obj);

        // Assert
        Assert.True(result, "Dla typu AllPartners 'Value' musi być null lub puste");
    }

    [Fact]
    public void Validate_ForTypeAllPartnersAndNonEmptyValue_ReturnsFalse()
    {
        // Arrange
        ValidatableObject obj = new() { Type = ValidationType.AllPartners, Value = "jakaś wartość" };

        // Act
        bool result = TypeValueValidator.Validate(obj);

        // Assert
        Assert.False(result, "Dla typu AllPartners niepusta wartość powinna być niepoprawna.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Dowolna wartość")]
    public void Validate_WhenNoMatchingRuleExists_ReturnsTrue(string? value)
    {
        // Arrange
        ValidatableObject obj = new() { Type = ValidationType.NoMatchingRule, Value = value };

        // Act
        bool result = TypeValueValidator.Validate(obj);

        // Assert
        Assert.True(result, "Brak reguły walidacyjnej powinien być traktowany jako sukces.");
    }

    [Fact]
    public void Validate_WhenTypePropertyIsMissing_ThrowsArgumentException()
    {
        // Arrange
        ObjectWithoutType obj = new() { Value = "test" };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TypeValueValidator.Validate(obj));
        Assert.Contains("Właściwość 'Type' nie została znaleziona", exception.Message);
    }

    [Fact]
    public void Validate_WhenValuePropertyIsMissing_ThrowsArgumentException()
    {
        // Arrange
        ObjectWithoutValue obj = new() { Type = ValidationType.Nip };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TypeValueValidator.Validate(obj));
        Assert.Contains("Właściwość 'Value' nie została znaleziona", exception.Message);
    }

    [Fact]
    public void Validate_WhenTypePropertyIsNotEnum_ThrowsArgumentException()
    {
        // Arrange
        ObjectWithWrongTypeProperty obj = new() { Type = 123, Value = "test" };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TypeValueValidator.Validate(obj));
        Assert.Contains("nie jest typu Enum", exception.Message);
    }

    [Fact]
    public void Validate_WhenTypePropertyIsNull_ThrowsArgumentException()
    {
        // Arrange
        ValidatableObject obj = new() { Type = null, Value = "test" };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TypeValueValidator.Validate(obj));
        Assert.Contains("nie jest typu Enum", exception.Message);
    }
}

