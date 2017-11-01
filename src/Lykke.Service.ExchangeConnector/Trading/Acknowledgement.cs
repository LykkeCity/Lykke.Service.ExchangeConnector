namespace TradingBot.Trading
{
    public class Acknowledgement
    {
        public string ClientOrderId { get; set; }
        
        public string ExchangeOrderId { get; set; }
        
        public bool Success { get; set; }
        
        public string Message { get; set; }
        
        public string Exchange { get; set; }
        
        public string Instrument { get; set; }
        
        public AcknowledgementFailureType FailureType { get; set; }

        public override string ToString()
        {
            return $"ClientId: {ClientOrderId}, ExternalId: {ExchangeOrderId}, Success: {Success}, {Exchange}, {Instrument}";
        }
    }
}
