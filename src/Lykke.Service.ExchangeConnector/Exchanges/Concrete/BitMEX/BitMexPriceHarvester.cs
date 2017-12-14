using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexPriceHarvester
    {
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private Func<TickPrice, Task> _tickPriceHandler;

        public BitMexPriceHarvester(
            string exchangeName,
            BitMexExchangeConfiguration configuration,
            IBitmexSocketSubscriber socketSubscriber,
            ILog log)
        {
            _log = log;
            socketSubscriber.Subscribe(BitmexTopic.quote, HandleResponseAsync);
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols, exchangeName);
        }

        public void AddHandler(Func<TickPrice, Task> handler)
        {
            _tickPriceHandler = handler;
        }

        private async Task HandleResponseAsync(TableResponse table)
        {
            if (_tickPriceHandler == null)
            {
                throw new InvalidOperationException("Tick price handler is not set.");
            }

            if (!ValidateQuote(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexPriceHarvester), nameof(HandleResponseAsync),
                    $"Ignoring invalid 'quote' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            if (table.Action == Action.Insert)
            {
                var prices = table.Data.Select(q => _mapper.QuoteToModel(q));
                foreach (var price in prices)
                {
                    await _tickPriceHandler(price);
                }
            }
        }

        private bool ValidateQuote(TableResponse table)
        {
            return table != null
                   && table.Data != null
                   && table.Data.All(item => item.AskPrice.HasValue && item.BidPrice.HasValue);
        }
    }
}
