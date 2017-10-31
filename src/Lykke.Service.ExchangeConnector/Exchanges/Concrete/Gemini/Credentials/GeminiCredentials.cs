namespace TradingBot.Exchanges.Concrete.Gemini.Credentials
{
    internal sealed class GeminiCredentials
    {
        public string ApiKey { get; private set; }

        public string PayLoad { get; private set; }
        
        public string Signature { get; private set; }

        public GeminiCredentials(string apiKey, string payLoad, string signature)
        {
            ApiKey = apiKey;
            PayLoad = payLoad;
            Signature = signature;
        }
    }
}
