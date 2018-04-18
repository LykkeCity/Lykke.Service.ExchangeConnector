using System;
using Newtonsoft.Json;

namespace TradingBot.Trading
{
	public class TickPrice
	{
        public TickPrice(Instrument instrument, DateTime time, decimal mid)
		{
		    Instrument = instrument;
		    
			Time = time;
			Ask = mid;
		    Bid = mid;
			Mid = mid;
		}

		[JsonConstructor]
		public TickPrice(Instrument instrument, DateTime time, decimal ask, decimal bid)
		{
		    Instrument = instrument;
		    
			Time = time;
			Ask = ask;
			Bid = bid;
		    
		    Mid = (ask + bid) / 2m;
		}

	    public Instrument Instrument { get; }
	    
		public DateTime Time { get; }

	    [JsonProperty("ask")]
        public decimal Ask { get; }

	    [JsonProperty("bid")]
        public decimal Bid { get; }

		[JsonIgnore]
		public decimal Mid { get; }

        [JsonProperty("source")]
        public string NewFormartSource { get; set; }

	    [JsonProperty("asset")]
	    public string NewFormartAsset => Instrument.Name;

	    [JsonProperty("timestamp")]
	    public DateTime NewFormartTimestamp => Time;

        public override string ToString()
		{
			return $"TickPrice for {Instrument}: Time={Time}, Ask={Ask}, Bid={Bid}, Mid={Mid}";
		}
	}
}
