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

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj as Instrument)?.Name.Equals(Name) ?? false;
        }
    }
}
