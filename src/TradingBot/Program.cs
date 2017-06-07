using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.IO;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using TradingBot.Trading;
using TradingBot.Common.Infrastructure;

namespace TradingBot
{
    class Program
    {
        private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddCommandLine(args);
            
            Configuration config = Configuration.FromConfigurationRoot(configBuilder.Build());

            var exchange = ExchangeFactory.CreateExchange(config.ExchangeConfig);
            var instruments = new [] { new Instrument(config.ExchangeConfig.Instrument)};

            var cycle = new GetPricesCycle(exchange, instruments);

            var task = cycle.Start(config.RabbitMQConfig);

            Logger.LogInformation("Press Ctrl+C for exit");

            Console.CancelKeyPress += (sender, eventArgs) => 
	            {
					eventArgs.Cancel = true; // Don't terminate the process immediately, wait for the Main thread to exit gracefully.

					cycle.Stop();

					if (task.Status == TaskStatus.Running)
					{
                        Logger.LogInformation("Waiting for prices cycle completion");
						task.Wait();
					}	
	            };


            task.Wait();

            Logger.LogInformation("Applicatoin stopped.");
            Environment.Exit(0);



            //Logger.LogInformation("Get historical data");

            //var getCandlesTask = instrumentsApi.GetCandles("EUR_USD", DateTime.UtcNow.AddDays(-1), null, CandlestickGranularity.S30);
            //var candlesResponse = getCandlesTask.Result;

            //var eurUsdHistorical = new AlphaEngineAgent(new Trading.Instrument("EUR_USD"));

            //foreach (var candle in candlesResponse.Candles)
            //{
            //    eurUsdHistorical.OnPriceChange(new TickPrice(candle.Time, candle.Mid.Closing));
            //}

            //Logger.LogInformation($"Total value: {eurUsdHistorical.GetCumulativePosition().Money}");



            //Logger.LogInformation("Opening stream...");

            //var instrumentsToProcess = instruments.Instruments.Select(x => x.Name).Take(10).ToArray();

            //var agents = instrumentsToProcess
            //    .ToDictionary(x => x, x => new AlphaEngineAgent(new Trading.Instrument(x)));

            //var task = Task.Run(() => {
            //    pricesApi.OpenPricesStream(accountId, ctSourse.Token,
            //        price => {
            //            Logger.LogInformation($"Price received: {price}");
            //            agents[price.Instrument].OnPriceChange(new TickPrice(price.Time, price.CloseoutAsk, price.CloseoutBid));
            //        },
            //        heartbeat => {
            //            Logger.LogInformation($"Heartbeat received: {heartbeat}");
            //        },
            //        instrumentsToProcess
            //    ).Wait();
            //});

            
            //Logger.LogInformation("Trading results:");
            //foreach (var agent in agents)
            //{
            //    Logger.LogInformation($"{agent.Key}: {agent.Value.GetCumulativePosition().Money}");
            //}
            
        }
    }
}
