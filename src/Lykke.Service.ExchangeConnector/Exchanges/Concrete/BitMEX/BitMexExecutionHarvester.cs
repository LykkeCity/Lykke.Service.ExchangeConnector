using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using Newtonsoft.Json;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexExecutionHarvester : IStartable, IStopable
    {
        private readonly IBitmexSocketSubscriber _socketSubscriber;
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private readonly IHandler<ExecutionReport> _tradeHandler;

        public BitMexExecutionHarvester(BitMexExchangeConfiguration configuration, IBitmexSocketSubscriber socketSubscriber, ILog log, IHandler<ExecutionReport> tradeHandler)
        {
            _socketSubscriber = socketSubscriber;
            _tradeHandler = tradeHandler;
            _log = log.CreateComponentScope(nameof(BitMexExecutionHarvester));
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols);
        }

        private async Task HandleExecutionResponseAsync(TableResponse table)
        {
            if (_tradeHandler == null)
            {
                throw new InvalidOperationException("Acknowledgment handler or executed trader is not set.");
            }

            if (!ValidateOrder(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexExecutionHarvester), nameof(HandleExecutionResponseAsync),
                    $"Ignoring invalid 'order' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            switch (table.Action)
            {
                case Action.Insert:
                    var acks = table.Data.Select(row => _mapper.OrderToTrade(row));
                    foreach (var ack in acks)
                    {
                        if (ack.ExecutionStatus != OrderExecutionStatus.Fill)
                        {
                            continue;
                        }
                        await _tradeHandler.Handle(ack);
                    }
                    break;
                case Action.Partial:
                    //  Just ignore
                    break;
                default:
                    await _log.WriteWarningAsync(nameof(HandleExecutionResponseAsync), "Execution response", $"Unexpected response {table.Action}");
                    break;
            }
        }

        private static bool ValidateOrder(TableResponse table)
        {
            return table?.Data != null && table.Data.All(item =>
                       !string.IsNullOrEmpty(item.Symbol)
                       && !string.IsNullOrEmpty(item.OrderID));
        }

        public void Start()
        {
            _socketSubscriber.Subscribe(BitmexTopic.execution, HandleExecutionResponseAsync);
            _socketSubscriber.Start();
        }

        public void Dispose()
        {
            _socketSubscriber.Stop();
            _socketSubscriber.Dispose();
        }

        public void Stop()
        {
            _socketSubscriber.Stop();
        }
    }
}
