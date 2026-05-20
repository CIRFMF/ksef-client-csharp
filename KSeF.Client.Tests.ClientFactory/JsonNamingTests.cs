using KSeF.Client.DI;
using KSeF.Client.ClientFactory.DI;
using KSeF.Client.Http;
using Microsoft.Extensions.DependencyInjection;

namespace KSeF.Client.Tests.ClientFactory
{
    public class JsonNamingTests
    {
        [Theory]
        [InlineData(true, "\"someValue\"")]
        [InlineData(false, "\"SomeValue\"")]
        public void AddKSeFClient_WhenOptionSetDirectly_JsonMatchesSetting(bool useCamelCaseForRequests, string expectedValue)
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();

            // Act: explicitly set UseCamelCaseForRequests = false
            services.AddKSeFClient(opts =>
            {
                opts.BaseUrl = "https://example.test";
                opts.UseCamelCaseForRequests = useCamelCaseForRequests;
            });

            try
            {
                var obj = new { SomeValue = "x" };
                string json = JsonUtil.Serialize(obj);

                // Assert: PascalCase should be preserved when option is false
                Assert.Contains(expectedValue, json);
            }
            finally
            {
                // Reset global serializer policy to default (PascalCase)
                JsonUtil.ResetConfigurationForCasePropertyName(false);
            }
        }

        [Fact]
        public void AddKSeFClient_WhenCalledWithFalseAfterTrue_JsonReturnsToPascalCase()
        {
            // Arrange
            JsonUtil.ResetConfigurationForCasePropertyName(true);

            try
            {
                ServiceCollection services = new ServiceCollection();

                // Act
                services.AddKSeFClient(opts =>
                {
                    opts.BaseUrl = "https://example.test";
                    opts.UseCamelCaseForRequests = false;
                });

                AssertJsonPropertyName("\"SomeValue\"", "\"someValue\"");
            }
            finally
            {
                JsonUtil.ResetConfigurationForCasePropertyName(false);
            }
        }

        [Theory]
        [InlineData(true, "\"someValue\"", "\"SomeValue\"")]
        [InlineData(false, "\"SomeValue\"", "\"someValue\"")]
        public void RegisterKSeFClientFactory_WhenOptionSetDirectly_JsonMatchesSetting(
            bool useCamelCase,
            string expectedValue,
            string unexpectedValue)
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();

            try
            {
                // Act
                services.RegisterKSeFClientFactory(useCamelCase);

                // Assert
                AssertJsonPropertyName(expectedValue, unexpectedValue);
            }
            finally
            {
                JsonUtil.ResetConfigurationForCasePropertyName(false);
            }
        }

        [Fact]
        public void RegisterKSeFClientFactory_WhenCalledWithFalseAfterTrue_JsonReturnsToPascalCase()
        {
            // Arrange
            JsonUtil.ResetConfigurationForCasePropertyName(true);

            try
            {
                ServiceCollection services = new ServiceCollection();

                // Act
                services.RegisterKSeFClientFactory(useCamelCase: false);

                AssertJsonPropertyName("\"SomeValue\"", "\"someValue\"");
            }
            finally
            {
                JsonUtil.ResetConfigurationForCasePropertyName(false);
            }
        }

        private static void AssertJsonPropertyName(string expectedValue, string unexpectedValue)
        {
            var obj = new { SomeValue = "x" };
            string json = JsonUtil.Serialize(obj);

            Assert.Contains(expectedValue, json);
            Assert.DoesNotContain(unexpectedValue, json);
        }
    }
}
