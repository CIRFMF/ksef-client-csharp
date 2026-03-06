using KSeF.Client.DI;
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
    }
}
