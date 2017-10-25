using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public interface ICurrencyMappingProvider
    {
        IDictionary<string, string> CurrencyMapping { get; }
    }
}
