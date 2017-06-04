using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TradingBot.Infrastructure;
using TradingBot.Exchanges.Concrete.StubImplementation;
using System.Collections.Generic;
using TradingBot.Trading;

namespace TradingBot
{
    class Program
    {
        private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            Configuration config;

            if (args == null || args.Length == 0)
            {
                Logger.LogInformation("TradingBot started without arguments.\n Use default settings.");
                config = Configuration.CreateDefaultConfig();
            }
            else
            {
                // TODO: parse exchange name and pairs list
				Logger.LogInformation("Unsupported argument. Use default settings.");
				config = Configuration.CreateDefaultConfig();
            }

            var exchange = config.Exchange;
            var instruments = config.Instruments.ToArray();

            var cycle = new GetPricesCycle(exchange, instruments);

            var task = cycle.Start(config.RabbitMQConfig);

            Logger.LogInformation("Press Ctrl+C for exit");

            Console.CancelKeyPress += (sender, eventArgs) => 
	            {
					cycle.Stop();

					if (task.Status == TaskStatus.Running)
					{
						Console.WriteLine("Waiting for prices cycle completion");
						task.Wait();
					}

					// Don't terminate the process immediately, wait for the Main thread to exit gracefully.
					//eventArgs.Cancel = true;
	            };


            task.Wait();

            Console.WriteLine("Applicatoin stopped.");
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
