using System;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests
{
    public class TradingSignalConverterTests
    
    {
        [Fact]
        public void SerializeAndDeserializeTradingSignal()
        {
            var converter = new GenericRabbitModelConverter<InstrumentTradingSignals>();
            var signal = new TradingSignal("", OrderCommand.Create, TradeType.Buy, 100.2m, 10.1m, DateTime.Now, OrderType.Limit);
            var instrumentSignals = new InstrumentTradingSignals(new Instrument("Exchange", "EURUSD"), new [] { signal });

            var serialized = converter.Serialize(instrumentSignals);
            Assert.NotNull(serialized);

            var deserialized = converter.Deserialize(serialized).TradingSignals[0];

            Assert.Equal(signal.Type, deserialized.Type);
            Assert.Equal(signal.Volume, deserialized.Volume);
            Assert.Equal(signal.OrderType, deserialized.OrderType);
            Assert.Equal(signal.Time, deserialized.Time);
            Assert.Equal(signal.OrderId, deserialized.OrderId);
            Assert.Equal(signal.Command, deserialized.Command);
        }
    }
}