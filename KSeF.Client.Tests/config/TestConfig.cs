using Microsoft.Extensions.Configuration;

namespace KSeF.Client.Tests.config
{
    public class TestConfig
    {
        public static IConfigurationRoot Load()
            => new ConfigurationBuilder()
                .AddUserSecrets<TestConfig>() 
                .Build();
    }
}
