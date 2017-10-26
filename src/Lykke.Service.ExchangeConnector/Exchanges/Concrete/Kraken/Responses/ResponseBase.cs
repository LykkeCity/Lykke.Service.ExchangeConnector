namespace TradingBot.Exchanges.Concrete.Kraken.Responses
{
    public class ResponseBase<TResult>
    {
        public string[] Error { get; set; }

        public TResult Result { get; set; }
    }
}
