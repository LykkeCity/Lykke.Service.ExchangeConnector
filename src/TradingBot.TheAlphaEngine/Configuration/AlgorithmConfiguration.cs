namespace TradingBot.TheAlphaEngine.Configuration
{
    public class AlgorithmConfiguration
    {
        public AlgorithmImplementation Implementation { get; set; }
    }

    public enum AlgorithmImplementation
    {
        Stub,
        AlphaEngine,
        AlphaEngineJavaPort
    }
}