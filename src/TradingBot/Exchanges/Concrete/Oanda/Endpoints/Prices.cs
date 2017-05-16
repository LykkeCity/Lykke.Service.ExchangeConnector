using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Prices;
using TradingBot.Infrastructure;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
{
    public class Prices : BaseApi
    {
        private readonly ILogger Logger = Logging.CreateLogger<Prices>();

        public Prices(ApiClient apiClient) : base(apiClient)
        {
        }

        public async Task OpenPricesStream(string accountId, CancellationToken cancellationToken, 
            Action<Price> priceCallback,
            Action<PriceHeartbeat> heartbeatCallback,
            params string[] instruments)
        {
            var query = "instruments=" + string.Join("%2C", instruments);

            using (var stream = await ApiClient.MakeStreamRequestAsync($"{OandaUrls.StreamApiBase}/v3/accounts/{accountId}/pricing/stream?{query}"))
            {
                using (var reader = new StreamReader(stream))
                {
                    while(!cancellationToken.IsCancellationRequested)
                    {
                        var line = reader.ReadLine();

                        if (line.Contains("HEARTBEAT"))
                        {
                            var heartbeat = JsonConvert.DeserializeObject<PriceHeartbeat>(line);
                            heartbeatCallback(heartbeat);
                        }
                        else if (line.Contains("PRICE"))
                        {
                            var price = JsonConvert.DeserializeObject<Price>(line);
                            priceCallback(price);
                        }
                    }
                    Logger.LogInformation("Canellation requested.");
                }
            }
        }
    }
}
