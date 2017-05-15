using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.AlphaEngine;
using TradingBot.Exchanges.OandaApi;
using TradingBot.Exchanges.OandaApi.ApiEndpoints;
using TradingBot.Infrastructure;

namespace TradingBot
{
    class Program
    {
        private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            Logger.LogInformation("Connecting to OANDA api...");

            var client = new ApiClient(OandaAuth.Token);
            var accountsApi = new AccountsApi(client);

            var accountsList = accountsApi.GetAccounts().Result;
            Logger.LogInformation($"Received {accountsList.Accounts.Count} accounts");

            var accountId = accountsList.Accounts.First().Id;
            
            var details = accountsApi.GetAccountDetails(accountId).Result;
            Logger.LogInformation($"Balance: {details.Account.Balance}");

            var instruments = accountsApi.GetAccountInstruments(accountId).Result;
            //Console.WriteLine($"Available instruments: {instruments.Instruments.Count}");

            Logger.LogDebug("Opening stream...");

            var pricesApi = new PricesApi(client);

            var instrumentsToProcess = instruments.Instruments.Select(x => x.Name).Take(10).ToArray();
            var instrumentAgents = instrumentsToProcess
                .ToDictionary(x => x, x => new InstrumentAgent(x, AlphaEngineConfig.DirectionalChangeThreshold));
            
            var ctSourse = new CancellationTokenSource();
            
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
