using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

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

        private bool stopRequested;

        public async Task Start(RabbitMQConfiguration rabbitConfig)
        {
            stopRequested = false;

            Logger.LogInformation($"Price cycle starting for exchange {exchange.Name}...");

            bool connectionTestPassed = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(10)).ConnectAsync(exchange.TestConnection);

            if (!connectionTestPassed)
            {
                Logger.LogError($"Price cycle not started: no connection to exchange {exchange.Name}");
                return;
            }

            using(var rabbit = new RabbitMQClient(rabbitConfig))
            {
                bool connected = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(15)).Connect(rabbit.OpenConnection);
                if (!connected)
                    return;


				var task = exchange.OpenPricesStream(instruments,
					 tickPrices => 
		                {
                            string message = string.Join<TickPrice>(", ", tickPrices);
		                    Logger.LogDebug($"{DateTime.Now}. Prices received: {message}");
		                    rabbit.SendMessage(message);
		                });

				while (!stopRequested)
				{
					await Task.Delay(TimeSpan.FromSeconds(5));
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
            stopRequested = true;

            exchange.ClosePricesStream();
        }
    }
}
