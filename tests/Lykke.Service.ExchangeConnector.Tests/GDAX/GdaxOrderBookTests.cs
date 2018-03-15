using Common.Log;
using Moq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.GDAX
{
    public class GdaxOrderBookTests
    {
        private readonly ILog _log;
        private readonly GdaxExchangeConfiguration _gdaxConfiguration;
        private readonly IHandler<OrderBook> _orderBookHandler;

        public GdaxOrderBookTests()
        {
            _log = new LogToConsole();

            var settingsManager = GdaxHelpers.GetGdaxSettingsMenager();
            _gdaxConfiguration = settingsManager.CurrentValue;

            _orderBookHandler = new Mock<IHandler<OrderBook>>().Object;

        }

        [Fact]
        public async Task HarvestAndPersist()
        {
            var orderBookHarvester = new GdaxOrderBooksHarvester(
                _gdaxConfiguration, _log,
                _orderBookHandler);
            orderBookHarvester.Start();

            await Task.Delay(1000000);

            orderBookHarvester.Stop();
        }
    }
}
