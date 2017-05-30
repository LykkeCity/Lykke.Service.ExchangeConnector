using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.AlphaEngine;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda;
using TradingBot.Exchanges.Concrete.Oanda.Endpoints;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Instruments;
using TradingBot.Infrastructure;
using TradingBot.Trading;

namespace TradingBot
{
    class Program
    {
        private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            Logger.LogInformation("Connecting to OANDA api...");

            var client = new ApiClient(OandaHttpClient.CreateHttpClient(OandaAuth.Token));
            var accountsApi = new Accounts(client);
            var pricesApi = new Prices(client);
            var instrumentsApi = new Instruments(client);


            var accountsList = accountsApi.GetAccounts().Result;
            Logger.LogInformation($"Received {accountsList.Accounts.Count} accounts");

            var accountId = accountsList.Accounts.First().Id;

            var details = accountsApi.GetAccountDetails(accountId).Result;
            Logger.LogInformation($"Balance: {details.Account.Balance}");

            var instruments = accountsApi.GetAccountInstruments(accountId).Result;
            Logger.LogInformation($"{instruments.Instruments.Count} instruments available for account");




            Logger.LogInformation("Get historical data");

            var getCandlesTask = instrumentsApi.GetCandles("EUR_USD", DateTime.UtcNow.AddDays(-1), null, CandlestickGranularity.S30);
            var candlesResponse = getCandlesTask.Result;

            var eurUsdHistorical = new AlphaEngineAgent(new Trading.Instrument("EUR_USD"));

            foreach (var candle in candlesResponse.Candles)
            {
                eurUsdHistorical.OnPriceChange(new TickPrice(candle.Time, candle.Mid.Closing));
            }

            Logger.LogInformation($"Total value: {eurUsdHistorical.GetCumulativePosition().Money}");


            var ctSourse = new CancellationTokenSource();

            Logger.LogInformation("Opening stream...");

            var instrumentsToProcess = instruments.Instruments.Select(x => x.Name).Take(10).ToArray();

            var agents = instrumentsToProcess
                .ToDictionary(x => x, x => new AlphaEngineAgent(new Trading.Instrument(x)));

            var task = Task.Run(() => {
                pricesApi.OpenPricesStream(accountId, ctSourse.Token,
                    price => {
                        Logger.LogInformation($"Price received: {price}");
                        agents[price.Instrument].OnPriceChange(new TickPrice(price.Time, price.CloseoutAsk, price.CloseoutBid));
                    },
                    heartbeat => {
                        Logger.LogInformation($"Heartbeat received: {heartbeat}");
                    },
                    instrumentsToProcess
                ).Wait();
            });


            Console.ReadLine();

            ctSourse.Cancel();

            task.Wait();

            Logger.LogInformation("Trading results:");
            foreach (var agent in agents)
            {
                Logger.LogInformation($"{agent.Key}: {agent.Value.GetCumulativePosition().Money}");
            }

            Console.ReadLine();
        }
    }
}
