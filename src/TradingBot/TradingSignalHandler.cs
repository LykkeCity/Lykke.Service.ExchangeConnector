using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Logging;

namespace TradingBot
{
    public class TradingSignalHandler
    {
        private readonly ILogger logger = Logging.CreateLogger<TradingSignalHandler>();
        
        private readonly Exchange exchange;
        
        private long signalsCounter;
        
        public TradingSignalHandler(Exchange exchange)
        {
            this.exchange = exchange;
        }

        public Task Handle(InstrumentTradingSignals tradingSignals)
        {
            if (++signalsCounter % 1000 == 0)
            {
                logger.LogDebug($"{signalsCounter}'s signal was received. Current PnL: " +
                                $"{string.Join(", ", exchange.Positions.Select(x => x.Key + ": " + x.Value.RealizedPnL))}");
            }
            
            return exchange.PlaceTradingOrders(tradingSignals);
        }
    }
}