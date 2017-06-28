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
            var converter = new TradingSignalConverter();
            var signal = new TradingSignal(SignalType.Long, 100.2m, 10.1m, DateTime.Now, OrderType.Limit);

            var serialized = converter.Serialize(signal);
            Assert.NotNull(serialized);

            var deserialized = converter.Deserialize(serialized);

            Assert.Equal(signal.Amount, deserialized.Amount);
            Assert.Equal(signal.Type, deserialized.Type);
            Assert.Equal(signal.Count, deserialized.Count);
            Assert.Equal(signal.OrderType, deserialized.OrderType);
            Assert.Equal(signal.Time, deserialized.Time);
        }
    }
}