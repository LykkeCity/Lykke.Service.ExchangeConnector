using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Exchanges.Concrete.GDAX.Entities
{
    internal class GdaxQueueOrderItem
    {
        public long SequenceNumber { get; }

        public OrderBookItem OrderBookItem { get; }

        public OrderBookEventType OrderBookEventType { get; }

        public GdaxQueueOrderItem(long sequenceNumber,
            OrderBookEventType orderBookEventType, OrderBookItem orderBookItem)
        {
            SequenceNumber = sequenceNumber;
            OrderBookItem = orderBookItem;
            OrderBookEventType = orderBookEventType;
        }
    }
}
