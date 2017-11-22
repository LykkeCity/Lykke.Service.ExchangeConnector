namespace TradingBot.Exchanges.Concrete.GDAX.Credentials
{
    internal sealed class GdaxCredentials
    {
        public string ApiKey { get; private set; }

        public string ApiSecret { get; private set; }

        public string PassPhrase { get; private set; }

        public string UnixTimestampString { get; private set; }

        public string Signature { get; private set; }

        public GdaxCredentials(string apiKey, string apiSecret, string passPhrase, 
            string unixTimestampString, string signature)
        {
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            PassPhrase = passPhrase;
            UnixTimestampString = unixTimestampString;
            Signature = signature;
        }
    }
}
