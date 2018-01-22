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
            var converter = new GenericRabbitModelConverter<TradingSignal>();
            var signal = new TradingSignal(new Instrument("Exchange", "EURUSD"),  "", OrderCommand.Create, TradeType.Buy, 100.2m, 10.1m, DateTime.UtcNow, OrderType.Limit);

            var serialized = converter.Serialize(signal);
            Assert.NotNull(serialized);

            var deserialized = converter.Deserialize(serialized);

            Assert.Equal(signal.TradeType, deserialized.TradeType);
            Assert.Equal(signal.Volume, deserialized.Volume);
            Assert.Equal(signal.OrderType, deserialized.OrderType);
            Assert.Equal(signal.Time, deserialized.Time);
            Assert.Equal(signal.OrderId, deserialized.OrderId);
            Assert.Equal(signal.Command, deserialized.Command);
        }

        [Fact]
        public void TradingSignal_IsTimeInThreshold()
        {
            var signal = new TradingSignal(null, "", OrderCommand.Create, TradeType.Buy, 100m, 100m, DateTime.UtcNow.AddMinutes(-5));
            
            Assert.True(signal.IsTimeInThreshold(TimeSpan.FromMinutes(6)));
            Assert.False(signal.IsTimeInThreshold(TimeSpan.FromMinutes(4)));
            
            var signalInFuture = new TradingSignal(null, "", OrderCommand.Create, TradeType.Buy, 100m, 100m, DateTime.UtcNow.AddMinutes(5));
            
            Assert.True(signalInFuture.IsTimeInThreshold(TimeSpan.FromMinutes(6)));
            Assert.False(signalInFuture.IsTimeInThreshold(TimeSpan.FromMinutes(4)));
        }
    }
}
