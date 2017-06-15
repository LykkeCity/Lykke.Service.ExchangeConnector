namespace TradingBot.Trading
{
    public class Instrument
    {
        public Instrument(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return string.Format("[Instrument: Name={0}]", Name);
        }
    }
}
