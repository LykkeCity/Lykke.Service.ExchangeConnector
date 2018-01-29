using Lykke.SettingsReader.Attributes;

namespace TradingBot.Infrastructure.Configuration
{
    public class CurrencySymbol
    {
        public string LykkeSymbol { get; set; }

        public string ExchangeSymbol { get; set; }

        [Optional]
        public bool OrderBookVolumeInQuoteCcy { get; set; }
    }
}
