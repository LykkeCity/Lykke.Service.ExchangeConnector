namespace TradingBot.Common.Trading
{
    public class Instrument
    {
        public Instrument(string exchange, string name)
        {
            Name = name;
            Exchange = exchange;
        }

        public string Name { get; }
        
        public string Exchange { get; }

        public override string ToString()
        {
            return $"{Name} on {Exchange}";
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Exchange.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((obj as Instrument)?.Name.Equals(Name) ?? false)
                   && ((Instrument) obj).Exchange.Equals(Exchange);
        }
    }
}
