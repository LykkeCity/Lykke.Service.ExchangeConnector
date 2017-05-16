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

            

            var instrumentsToProcess = instruments.Instruments.Select(x => x.Name).Take(10).ToArray();
            var instrumentAgents = instrumentsToProcess
                .ToDictionary(x => x, x => new IntrinsicTime(x, AlphaEngineConfig.DirectionalChangeThreshold));
            
            Logger.LogInformation("Get historical data");

            var getCandlesTask = instrumentsApi.GetCandles("EUR_USD", DateTime.UtcNow.AddDays(-1), null, CandlestickGranularity.S30);
            var candlesResponse = getCandlesTask.Result;

            var eurUsdHistorical = new IntrinsicTime("EUR_USD", AlphaEngineConfig.DirectionalChangeThreshold);

            foreach(var candle in candlesResponse.Candles)
            {
                eurUsdHistorical.HandlePriceChange(candle.Mid.Closing, candle.Time);
            }


            var ctSourse = new CancellationTokenSource();

            Logger.LogInformation("Opening stream...");
            
            var task = Task.Run(() => {
                pricesApi.OpenPricesStream(accountId, ctSourse.Token,
                    price => {
                        Logger.LogInformation($"Price received: {price}");
                        instrumentAgents[price.Instrument].HandlePriceChange(price.CloseoutBid, price.Time);
                    },
                    heartbeat => {
                        Logger.LogInformation($"Heartbeat received: {heartbeat}");
                    },
                    instrumentsToProcess
                ).Wait();
            });

            Task.Delay(TimeSpan.FromMinutes(2)).Wait();

            ctSourse.Cancel();

            task.Wait();
        }
    }
}
