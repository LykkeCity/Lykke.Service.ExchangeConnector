using System;
using System.Threading;
using TradingBot.Exchanges.Concrete.ICMarkets;
using Xunit;

namespace TradingBot.Tests.ICMarketsTests
{
    public class ConnectToRabbitMQTests
    {
        [Fact]
        public void TryConnect()
        {
            var client = new RabbitMQClient();

            var ctSource = new CancellationTokenSource();

            var task = client.OpenConnection(ctSource.Token, bytes =>
            {
                Console.WriteLine("Message received");
            });

            ctSource.Cancel();

            task.Wait();
        }
    }
}
