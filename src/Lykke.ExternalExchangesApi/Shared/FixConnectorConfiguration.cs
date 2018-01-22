using System.IO;

namespace Lykke.ExternalExchangesApi.Shared
{
    public class FixConnectorConfiguration
    {
        public string Password { get; }
        public TextReader FixConfig { get; }

        public FixConnectorConfiguration(string password, TextReader fixConfig)
        {
            Password = password;
            FixConfig = fixConfig;
        }
    }
}
