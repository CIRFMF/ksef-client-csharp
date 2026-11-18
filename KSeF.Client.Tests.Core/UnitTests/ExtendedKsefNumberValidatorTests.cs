#nullable enable
using ExtendedKsefNumberValidator = KSeF.Client.Validation.KsefNumberValidator;

namespace KSeF.Client.Tests.Core.UnitTests;

public class ExtendedKsefNumberValidatorTests
{
    [Theory]
    [InlineData("1111111111-20260711-65ED02180000-37")]
    [InlineData("M123456789-20260711-65ED02180000-B4")]
    [InlineData("ABC1234567-20260711-65ED02180000-D8")]
    [InlineData("1111111111-20200101-65ED02180000-72")]
    public void IsValid_V35WithValidFormatAndChecksum_ReturnsTrue(string number)
    {
        Assert.True(ExtendedKsefNumberValidator.IsValid(number));
    }

    [Fact]
    public void IsValid_V35WithMatchingFormatAndChecksum_DoesNotValidateCalendarDate()
    {
        Assert.True(ExtendedKsefNumberValidator.IsValid("1111111111-20260231-65ED02180000-50"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("not-a-number")]
    [InlineData("1111111111-20260711-65ED02180000-36")]
    [InlineData("1111111111-20260711-65ed02180000-37")]
    [InlineData("ABC1234567-20260711-65ED02180000-d8")]
    [InlineData("1111111111-20260711-65ED02180000X37")]
    [InlineData("1111111111-20260711-65ED0218000-37")]
    [InlineData("1111111111-20261311-65ED02180000-37")]
    [InlineData(" 1111111111-20260711-65ED02180000-37")]
    [InlineData("1111111111-20260711-65ED02180000-37 ")]
    [InlineData("1111111111-20260711-65ED02180000-37\n")]
    public void IsValid_InvalidInput_ReturnsFalse(string? number)
    {
        Assert.False(ExtendedKsefNumberValidator.IsValid(number));
    }

    [Theory]
    [InlineData("1111111111-20240131-65ED02-180000-00")]
    [InlineData("1111111111-20240131-65ED02-180000-FF")]
    [InlineData("M123456789-20240131-65ED02-180000-00")]
    [InlineData("ABC1234567-20240131-65ED02-180000-00")]
    [InlineData("1111111111-20200229-65ED02-180000-00")]
    public void IsValid_V36BeforeCutoffWithAnyHexChecksum_ReturnsTrue(string number)
    {
        Assert.True(ExtendedKsefNumberValidator.IsValid(number));
    }

    [Theory]
    [InlineData("1111111111-20240201-65ED02-180000-00")]
    [InlineData("1111111111-20240202-65ED02-180000-00")]
    [InlineData("1111111111-20260711-65ED02-180000-00")]
    [InlineData("1111111111-20230229-65ED02-180000-00")]
    [InlineData("1111111111-20230431-65ED02-180000-00")]
    [InlineData("1111111111-20231301-65ED02-180000-00")]
    [InlineData("1111111111-20230001-65ED02-180000-00")]
    [InlineData("1111111111-20230100-65ED02-180000-00")]
    [InlineData("1111111111-20240131-65ED02-180000-GG")]
    [InlineData("1111111111-20240131-65ed02-180000-00")]
    public void IsValid_V36WithInvalidDateOrFormat_ReturnsFalse(string number)
    {
        Assert.False(ExtendedKsefNumberValidator.IsValid(number));
    }
}
