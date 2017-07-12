using System;
using TradingBot.Common.Trading;
using Xunit;

namespace TradingBot.Common.Tests
{
    public class TradingSignalConverterTests
    
    {
        [Fact]
        public void SerializeAndDeserializeTradingSignal()
        {
            var converter = new GenericRabbitModelConverter<InstrumentTradingSignals>();
            var signal = new TradingSignal(TradeType.Buy, 100.2m, 10.1m, DateTime.Now, OrderType.Limit);
            var instrumentSignals = new InstrumentTradingSignals(new Instrument("EURUSD"), new [] { signal });

            var serialized = converter.Serialize(instrumentSignals);
            Assert.NotNull(serialized);

            var deserialized = converter.Deserialize(serialized).TradingSignals[0];

            Assert.Equal(signal.Amount, deserialized.Amount);
            Assert.Equal(signal.Type, deserialized.Type);
            Assert.Equal(signal.Count, deserialized.Count);
            Assert.Equal(signal.OrderType, deserialized.OrderType);
            Assert.Equal(signal.Time, deserialized.Time);
        }
    }
}