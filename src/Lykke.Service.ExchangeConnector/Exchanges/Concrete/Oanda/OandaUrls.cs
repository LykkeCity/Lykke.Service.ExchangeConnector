namespace TradingBot.Exchanges.Concrete.Oanda
{
    internal static class OandaUrls
    {
        public static readonly string ApiBase = "https://api-fxpractice.oanda.com";

        public static readonly string Accounts = $"{ApiBase}/v3/accounts";

        public static readonly string Instruments = $"{ApiBase}/v3/instruments";

        public static readonly string StreamApiBase = "https://stream-fxpractice.oanda.com";
    }
}