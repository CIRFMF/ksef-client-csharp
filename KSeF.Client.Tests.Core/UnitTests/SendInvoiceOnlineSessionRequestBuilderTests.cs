using KSeF.Client.Api.Builders.Online;

namespace KSeF.Client.Tests.Core.UnitTests
{
    public class SendInvoiceOnlineSessionRequestBuilderTests
    {
        private const string ValidNip = "1234563218";
        private const string InvalidNip = "1234563219";

        private static ISendInvoiceOnlineSessionRequestBuilder CreateImpl(bool nonProd)
        {
            return SendInvoiceOnlineSessionRequestBuilderImpl.Create(nonProd);
        }

        [Fact]
        public void PodmiotNip_OnProdLike_Default_InvalidChecksum_Throws()
        {
            // Arrange: nonProdEnvironment=false => PROD-like => walidacja checksum włączona
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: false);

            // Act + Assert
            Assert.Throws<ArgumentException>(() => builder.WithPodmiot1Nip(InvalidNip));
            Assert.Throws<ArgumentException>(() => builder.WithPodmiot2Nip(InvalidNip));
            Assert.Throws<ArgumentException>(() => builder.WithPodmiot3Nip(InvalidNip));
            Assert.Throws<ArgumentException>(() => builder.WithPodmiotUpowaznionyNip(InvalidNip));
        }

        [Fact]
        public void PodmiotNip_OnProdLike_Default_ValidChecksum_Allows()
        {
            // Arrange
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: false);

            // Act (no throw)
            builder.WithPodmiot1Nip(ValidNip);
            builder.WithPodmiot2Nip(ValidNip);
            builder.WithPodmiot3Nip(ValidNip);
            builder.WithPodmiotUpowaznionyNip(ValidNip);
        }

        [Fact]
        public void PodmiotNip_OnNonProd_InvalidChecksum_Allows()
        {
            // Arrange: nonProdEnvironment=true => brak walidacji checksum
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: true);

            // Act (no throw)
            builder.WithPodmiot1Nip(InvalidNip);
            builder.WithPodmiot2Nip(InvalidNip);
            builder.WithPodmiot3Nip(InvalidNip);
            builder.WithPodmiotUpowaznionyNip(InvalidNip);
        }

        [Fact]
        public void PodmiotUpowaznionyNip_OnProdLike_Empty_IsAllowed()
        {
            // Arrange
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: false);

            // Act (no throw) – "jeśli występuje"
            builder.WithPodmiotUpowaznionyNip(null);
            builder.WithPodmiotUpowaznionyNip("");
            builder.WithPodmiotUpowaznionyNip("   ");
        }

        [Fact]
        public void Podmiot3InternalId_OnProdLike_Invalid_Throws()
        {
            // Arrange: PROD-like
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: false);

            // Act + Assert (invalid internalId format/checksum)
            Assert.Throws<ArgumentException>(() => builder.WithPodmiot3InternalId("123-456-7890124"));
        }

        [Fact]
        public void Podmiot3InternalId_OnProdLike_Missing_IsAllowed()
        {
            // Arrange
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: false);

            // Act (no throw) – "jeśli identyfikator występuje"
            builder.WithPodmiot3InternalId(null);
            builder.WithPodmiot3InternalId("");
            builder.WithPodmiot3InternalId("   ");
        }

        [Fact]
        public void Podmiot3InternalId_OnNonProd_Invalid_IsAllowed()
        {
            // Arrange: NON-PROD
            ISendInvoiceOnlineSessionRequestBuilder builder = CreateImpl(nonProd: true);

            // Act (no throw)
            builder.WithPodmiot3InternalId("123-456-7890124");
        }
    }
}