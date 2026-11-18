using KSeF.Client.Core;
using System;

namespace KSeF.Client.Tests.Core.UnitTests;

public class Crc8Tests
{
    [Theory]
    [InlineData("", "00")]
    [InlineData("123456789", "F4")]
    [InlineData("1111111111-IZ202607-65ED02180000", "E7")]
    public void CalculateChecksum_KnownVector_ReturnsExpectedChecksum(string input, string expected)
    {
        Assert.Equal(expected, Crc8.CalculateChecksum(input));
    }

    [Fact]
    public void CalculateChecksum_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Crc8.CalculateChecksum(null!));
    }
}
