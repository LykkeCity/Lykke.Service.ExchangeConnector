using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Configuration;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;
using Newtonsoft.Json;

namespace TradingBot
{
    public class GetPricesCycle
    {
        private readonly ILogger Logger = Logging.CreateLogger<GetPricesCycle>();
        readonly Instrument[] instruments;

        public GetPricesCycle(Exchange exchange, Instrument[] instruments)
        {
            this.instruments = instruments ?? throw new ArgumentNullException(nameof(instruments));
            this.exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        }

        private Exchange exchange;

        private CancellationTokenSource ctSource;

        public async Task Start(RabbitMQConfiguration rabbitConfig)
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            Logger.LogInformation($"Price cycle starting for exchange {exchange.Name}...");

            bool connectionTestPassed = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(10))
                .ConnectAsync(exchange.TestConnection, token);

            if (!connectionTestPassed)
            {
                Logger.LogError($"Price cycle not started: no connection to exchange {exchange.Name}");
                return;
            }

            using(var rabbit = new RabbitMQClient(rabbitConfig))
            {
                bool connected = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(15))
                    .Connect(rabbit.OpenConnection, token);
                
                if (!connected)
                    return;


				var task = exchange.OpenPricesStream(instruments,
					 tickPrices => 
		                {
                            string message = JsonConvert.SerializeObject(tickPrices);
                            Logger.LogDebug($"{DateTime.Now}. Prices received: {message}");
		                    rabbit.SendMessage(message);
		                });

                while (!token.IsCancellationRequested)
				{
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
					Logger.LogDebug($"GetPricesCycle Heartbeat: {DateTime.Now}");
				}

				if (task.Status == TaskStatus.Running)
				{
					task.Wait();
				}                
            }
        }

        public void Stop()
        {
            Logger.LogInformation("Stop requested");
            ctSource.Cancel();

            exchange.ClosePricesStream();
        }
    }
}
