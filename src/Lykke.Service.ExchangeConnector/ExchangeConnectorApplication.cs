using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges;
using TradingBot.Communications;

namespace TradingBot
{
    internal sealed class ExchangeConnectorApplication : IApplicationFacade
    {
        private readonly ILog _log;
        private readonly Timer _timer;
        private readonly IReadOnlyCollection<Exchange> _exchanges;


        public ExchangeConnectorApplication(TranslatedSignalsRepository translatedSignalsRepository, ExchangeFactory exchange, ILog log)
        {
            _log = log;

            _exchanges = exchange.CreateExchanges();
            _timer = new Timer(OnHeartbeat);
        }

        public async Task Start()
        {
            if (!_exchanges.Any())
            {
                await _log.WriteInfoAsync(
                    nameof(TradingBot),
                    nameof(ExchangeConnectorApplication),
                    nameof(Start),
                    "There is no enabled exchange.");
                return;
            }

            await _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Start),
                $"Price cycle starting for exchanges: {string.Join(", ", _exchanges.Select(e => e.Name))}...");


            foreach (var exchange in _exchanges)
            {
                try
                {

                    exchange.Start();
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(ExchangeConnectorApplication), nameof(Start), "Starting exchange", ex);
                    throw;
                }
            }

            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
        }

        private async void OnHeartbeat(object state)
        {
            await _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Start),
                $"Exchange connector heartbeat: {DateTime.Now}. Exchanges statuses: {string.Join(", ", GetExchanges().Select(x => $"{x.Name}: {x.State}"))}");
        }



        public void Stop()
        {
            _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Stop),
                "Stop requested")
                .Wait();

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            foreach (var exchange in _exchanges)
            {
                exchange.Stop();
            }
        }

        public IReadOnlyCollection<IExchange> GetExchanges()
        {
            return _exchanges;
        }

        public IExchange GetExchange(string name)
        {
            return _exchanges.FirstOrDefault(e => e.Name == name) ?? throw new ArgumentException(@"Invalid exchangeName", nameof(name));
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
