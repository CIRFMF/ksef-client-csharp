using KSeF.Client.Validation;
using KSeF.Client.Api.Builders.Online;

namespace KSeF.Client.Tests.Core.UnitTests
{
    public class IdentifierValidatorsTests
    {
        [Theory]
        [InlineData("1234563218", true)] // Valid NIP
        [InlineData("1234563219", false)] // Invalid checksum
        [InlineData("123456", false)] // Too short
        [InlineData("123456789012", false)] // Too long
        [InlineData("12345678A9", false)] // Contains non-digit
        public void IsValidNip_ValidatesCorrectly(string nip, bool expected)
        {
            // Act
            bool result = IdentifierValidators.IsValidNip(nip);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("6824515772-12343", true)]  // Valid InternalId
        [InlineData("123-456-7890124", false)] // Invalid checksum
        [InlineData("12345678901234", false)] // Too long
        [InlineData("123456789012", false)] // Too short
        [InlineData("123-456-78A0123", false)] // Contains non-digit
        public void IsValidInternalId_ValidatesCorrectly(string internalId, bool expected)
        {
            // Act
            bool result = IdentifierValidators.IsValidInternalId(internalId);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ValidateNip_ProductionEnvironment_ThrowsForInvalidNip()
        {
            // Arrange
            SendInvoiceOnlineSessionRequestBuilderImpl builder = (SendInvoiceOnlineSessionRequestBuilderImpl)SendInvoiceOnlineSessionRequestBuilderImpl.Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => builder.WithPodmiot1Nip("1234563219")));
        }

        [Fact]
        public void ValidateNip_NonProductionEnvironment_AllowsInvalidNip()
        {
            // Arrange
            SendInvoiceOnlineSessionRequestBuilderImpl builder = new SendInvoiceOnlineSessionRequestBuilderImpl(true);

            // Act & Assert
            builder.WithPodmiot1Nip("1234563219"); 
        }
    }
}