﻿namespace TradingBot.Infrastructure.Configuration
{
    public sealed class JfdCurrencySymbol : CurrencySymbol
    {
        public decimal InitialMarginPercent { get; set; }
        public decimal MaintMarginPercent { get; set; }
    }
}
