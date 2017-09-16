namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class MarketOrderResponse
    {
        public ErrorModel Error { get; set; }
    }

    public class ErrorModel {
        public ErrorCode Code { get; set; }

        public string Field { get; set; }

        public string Message { get; set; }

        public override string ToString() {
            return $"Code: {Code}, Field: {Field}, Message: {Message}";
        }
    }

    public enum ErrorCode {
        InvalidInputField, Ok, LowBalance, AlreadyProcessed, UnknownAsset, NoLiquidity, NotEnoughFunds, 
        Dust, ReservedVolumeHigherThanBalance, NotFound, BalanceLowerThanReserved, LeadToNegativeSpread, 
        PriceGapTooHigh, RuntimeError
    }
}