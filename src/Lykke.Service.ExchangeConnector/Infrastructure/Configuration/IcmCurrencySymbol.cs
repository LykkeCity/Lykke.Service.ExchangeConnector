namespace TradingBot.Infrastructure.Configuration
{
    public sealed class IcmCurrencySymbol : CurrencySymbol
    {
        public decimal InitialMarginPercent { get; set; }
        public decimal MaintMarginPercent { get; set; }
    }
}
