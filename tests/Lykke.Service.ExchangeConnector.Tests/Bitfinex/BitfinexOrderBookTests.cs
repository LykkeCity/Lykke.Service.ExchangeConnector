using Common.Log;
using Moq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Bitfinex
{
    public class BitfinexOrderBookTests
    {
        private readonly ILog _log;
        private readonly BitfinexExchangeConfiguration _bitfinexConfiguration;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<OrderBook> _orderBookHandler;

        public BitfinexOrderBookTests()
        {
            _log = new LogToConsole();

            var settingsManager = BitfinexHelpers.GetBitfinexSettingsMenager();
            _bitfinexConfiguration = settingsManager.CurrentValue;

            _orderBookHandler = new Mock<IHandler<OrderBook>>().Object;
            _tickPriceHandler = new Mock<IHandler<TickPrice>>().Object;

        }

        [Fact]
        public async Task HarvestTicker()
        {
            var orderBookHarvester = new BitfinexOrderBooksHarvester(_bitfinexConfiguration, _orderBookHandler, _tickPriceHandler, _log);

            var tickerTcs = new TaskCompletionSource<TickPrice>();

            orderBookHarvester.Start();

            await Task.WhenAny(Task.Delay(10000), tickerTcs.Task);

            orderBookHarvester.Stop();

            Assert.True(tickerTcs.Task.IsCompletedSuccessfully);
        }
    }
}
