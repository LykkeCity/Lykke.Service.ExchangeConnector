namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class AssetPair
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public int Accuracy { get; set; }
        
        public int InvertedAccuracy { get; set; }
        
        public string BaseAssetId { get; set; }
        
        public string QuotingAssetId { get; set; }
    }
}