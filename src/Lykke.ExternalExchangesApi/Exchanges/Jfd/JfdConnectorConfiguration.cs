using System.IO;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd
{
    public class JfdConnectorConfiguration
    {
        public string Password { get; }
        public TextReader FixConfig { get; }

        public JfdConnectorConfiguration(string password, TextReader fixConfig)
        {
            Password = password;
            FixConfig = fixConfig;
        }
    }
}
