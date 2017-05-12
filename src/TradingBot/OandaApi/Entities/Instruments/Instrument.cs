namespace TradingBot.OandaApi.Entities.Instruments
{
    public class Instrument
    {
        public string Name { get; set; }

        public InstrumentType Type { get; set; }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
