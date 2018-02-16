using Newtonsoft.Json;
using System;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.Ticks
{
    public class TickPrice
	{
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

		public decimal Ask { get; }

		public decimal Bid { get; }

		[JsonIgnore]
		public decimal Mid { get; }

		public override string ToString()
		{
			return $"TickPrice for {Instrument}: Time={Time}, Ask={Ask}, Bid={Bid}, Mid={Mid}";
		}
	}
}
