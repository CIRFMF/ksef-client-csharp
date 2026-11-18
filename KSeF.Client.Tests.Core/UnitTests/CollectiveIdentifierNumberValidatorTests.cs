#nullable enable
using KSeF.Client.Validation;

namespace KSeF.Client.Tests.Core.UnitTests;

public class CollectiveIdentifierNumberValidatorTests
{
    [Theory]
    [InlineData("1111111111-IZ202607-65ED02180000-E7")]
    [InlineData("1111111111-IZ202607-000000000000-8D")]
    public void IsValid_ValidFormatAndChecksum_ReturnsTrue(string number)
    {
        Assert.True(CollectiveIdentifierNumberValidator.IsValid(number));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("1111111111-IZ202607-65ED02180000-E6")]
    [InlineData("2111111111-IZ202607-65ED02180000-E7")]
    [InlineData("1111111111-IZ202607-65ED02180001-E7")]
    [InlineData("1111111111-IZ202607-65ed02180000-E7")]
    [InlineData("1111111111-IZ202607-65ED02180000-e7")]
    [InlineData("1111111111-iz202607-65ED02180000-E7")]
    [InlineData("1111111111-IX202607-65ED02180000-E7")]
    [InlineData("111111111-IZ202607-65ED02180000-E7")]
    [InlineData("1111111111-IZ202607-65ED0218000-E7")]
    [InlineData("1111111111-IZ202607-65ED02180000X-E7")]
    [InlineData("1111111111-IZ202607-65ED02180000E7")]
    [InlineData("١111111111-IZ202607-65ED02180000-E7")]
    [InlineData("1111111111-IZ٢٠٢٦07-65ED02180000-E7")]
    [InlineData(" 1111111111-IZ202607-65ED02180000-E7")]
    [InlineData("1111111111-IZ202607-65ED02180000-E7 ")]
    [InlineData("1111111111-IZ202607-65ED02180000-E7\n")]
    [InlineData("1111111111-IZ202607-65ED02180000-E7\r\n")]
    public void IsValid_InvalidInput_ReturnsFalse(string? number)
    {
        Assert.False(CollectiveIdentifierNumberValidator.IsValid(number));
    }

    [Theory]
    [InlineData("1111111111-IZ202600-65ED02180000-52")]
    [InlineData("0000000000-IZ202613-65ED02180000-7D")]
    public void IsValid_MatchingFormatAndChecksum_DoesNotValidateMonthOrNip(string number)
    {
        Assert.True(CollectiveIdentifierNumberValidator.IsValid(number));
    }

    [Fact]
    public void RegexPattern_ValidFormatWithInvalidChecksum_Matches()
    {
        Assert.Matches(RegexPatterns.CollectiveIdentifierNumber, "1111111111-IZ202607-65ED02180000-E6");
    }
}
