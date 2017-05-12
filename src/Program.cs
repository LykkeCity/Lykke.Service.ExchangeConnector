using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.AlphaEngine;
using TradingBot.OandaApi;
using TradingBot.OandaApi.ApiEndpoints;

namespace TradingBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting to OANDA api...");

            var client = new ApiClient(OandaAuth.Token);
            var accountsApi = new AccountsApi(client);

            var accountsList = accountsApi.GetAccounts().Result;
            Console.WriteLine($"Received {accountsList.Accounts.Count} accounts");

            var accountId = accountsList.Accounts.First().Id;
            
            var details = accountsApi.GetAccountDetails(accountId).Result;
            Console.WriteLine($"Balance: {details.Account.Balance}");

            var instruments = accountsApi.GetAccountInstruments(accountId).Result;
            //Console.WriteLine($"Available instruments: {instruments.Instruments.Count}");

            Console.WriteLine("Opening stream...");

            var pricesApi = new PricesApi(client);

            var alphaEngine = new PriceCurve("EUR_USD");

            var ctSourse = new CancellationTokenSource();
            
            var task = Task.Run(() => {
                pricesApi.OpenPricesStream(accountId, ctSourse.Token,
                    price => {
                        Console.WriteLine($"Price received: {price}");
                        alphaEngine.HandlePriceChange(price.CloseoutBid, price.Time);
                    },
                    heartbeat => {
                        Console.WriteLine($"Heartbeat received: {heartbeat}");
                    },
                    "EUR_USD"
                    //instruments.Instruments.Select(x => x.Name).Take(10).ToArray()
                ).Wait();
            });

            Task.Delay(TimeSpan.FromMinutes(2)).Wait();

            ctSourse.Cancel();

            task.Wait();
        }
    }
}
