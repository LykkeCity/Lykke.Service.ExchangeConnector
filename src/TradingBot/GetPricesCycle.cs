using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Polly;
using TradingBot.Common.Configuration;
using TradingBot.Common.Trading;
using TradingBot.Infrastructure.Logging;

namespace TradingBot
{
    public class GetPricesCycle
    {
        private readonly ILogger logger = Logging.CreateLogger<GetPricesCycle>();

        public GetPricesCycle(Configuration config)
        {
            this.config = config;

			exchange = ExchangeFactory.CreateExchange(config);
            tradingSignalHandler = new TradingSignalHandler(exchange);
        }


        private readonly Exchange exchange;
        private readonly TradingSignalHandler tradingSignalHandler;
		private CancellationTokenSource ctSource;
        private readonly Configuration config;        
        private RabbitMqSubscriber<InstrumentTradingSignals> signalSubscriber;
        

        public async Task Start()
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            if (exchange == null)
            {
                logger.LogInformation("There is no enabled exchange.");
                return;
            }
            
            logger.LogInformation($"Price cycle starting for exchange {exchange.Name}...");

            var retry = Policy
                .HandleResult<bool>(x => !x)
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(10));
            
            bool connectionTestPassed = await retry.ExecuteAsync(exchange.TestConnection, token);
            if (!connectionTestPassed)
            {
                logger.LogError($"Price cycle not started: no connection to exchange {exchange.Name}");
                return;
            }

            if (config.RabbitMq.Enabled)
            {
                SetupTradingSignalsSubscription(config.RabbitMq);
            }

            var task = exchange.OpenPricesStream();

            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
				logger.LogDebug($"GetPricesCycle Heartbeat: {DateTime.Now}");
			}

			if (task.Status == TaskStatus.Running)
			{
				task.Wait();
			}
        }

        private void SetupTradingSignalsSubscription(RabbitMqConfiguration rabbitConfig)
        {
            var subscriberSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = rabbitConfig.GetConnectionString(),
                ExchangeName = rabbitConfig.SignalsExchange,
                QueueName = rabbitConfig.QueueName
            };
            
            signalSubscriber = new RabbitMqSubscriber<InstrumentTradingSignals>(subscriberSettings)
                .SetMessageDeserializer(new InstrumentTradingSignalsConverter())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new RabbitConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(tradingSignalHandler.Handle)
                .Start();  
        }

        public void Stop()
        {
            logger.LogInformation("Stop requested");
            ctSource.Cancel();

            exchange?.ClosePricesStream();

            //((IStopable) rabbitPublisher)?.Stop();
        }

        public class RabbitConsole : IConsole
        {
            public void WriteLine(string line)
            {
                Console.WriteLine(line);
            }
        }
    }
}
