using Autofac;
using Common;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexOrderHarvester : IStartable, IStopable
    {
        private readonly IBitmexSocketSubscriber _socketSubscriber;
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private readonly IHandler<ExecutionReport> _tradeHandler;

        public BitMexOrderHarvester(
            BitMexExchangeConfiguration configuration,
            IBitmexSocketSubscriber socketSubscriber,
            IHandler<ExecutionReport> tradeHandler,
            ILog log)
        {
            _socketSubscriber = socketSubscriber;
            _log = log;
            _tradeHandler = tradeHandler;
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols, configuration);
        }



        private async Task HandleResponseAsync(TableResponse table)
        {
            if (_tradeHandler == null)
            {
                throw new InvalidOperationException("Acknowledgement handler or executed trader is not set.");
            }

            if (!ValidateOrder(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexOrderHarvester), nameof(HandleResponseAsync),
                    $"Ignoring invalid 'order' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            switch (table.Action)
            {
                case Action.Insert:
                case Action.Update:
                    foreach (var row in table.Data)
                    {
                        var trade = _mapper.OrderToTrade(row);

                        if (trade.ExecutionStatus == OrderExecutionStatus.Unknown)
                        {
                            await _log.WriteWarningAsync(nameof(BitMexOrderHarvester), nameof(HandleResponseAsync),
                                $"Can't convert trade status {row.OrdStatus} into ExecutionStatus. Converted item: {trade}. Don't call handlers.");
                        }
                        else
                        {
                            await _tradeHandler.Handle(trade);
                        }
                    }
                    break;
            }
        }

        private bool ValidateOrder(TableResponse table)
        {
            return table != null
                   && table.Data != null
                   && table.Data.All(item =>
                       !string.IsNullOrEmpty(item.Symbol)
                       && !string.IsNullOrEmpty(item.OrderID));
        }

        public void Start()
        {
            _socketSubscriber.Subscribe(BitmexTopic.order, HandleResponseAsync);
            _socketSubscriber.Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _socketSubscriber.Stop();
        }
    }
}
