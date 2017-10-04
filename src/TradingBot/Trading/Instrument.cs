﻿using Newtonsoft.Json;

namespace TradingBot.Trading
{
    public class Instrument
    {
        [JsonConstructor]
        public Instrument(string exchange, string name)
        {
            Name = name;
            Exchange = exchange;

            if (name.Length == 6)
            {
                Base = name.Substring(0, 3);
                Quote = name.Substring(3, 3);    
            }
            else if (name.StartsWith("LKK1Y"))
            {
                Base = "LKK1Y";
                Quote = name.Remove(0, 5);
            }
            else if (name.EndsWith("LKK1Y"))
            {
                Base = name.Substring(0, name.Length - 5);
                Quote = "LKK1Y";
            }
        }

        public Instrument(string exchange, string name, string @base, string quote) : this(exchange, name)
        {
            Base = @base;
            Quote = quote;
        }

        [JsonProperty("name")]
        public string Name { get; }
        
        [JsonProperty("exchange")]
        public string Exchange { get; }
        
        [JsonProperty("base")]
        public string Base { get; }
        
        [JsonProperty("quote")]
        public string Quote { get; }
        
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
        
        public static bool operator== (Instrument left, Instrument right) 
        {
            if ((object)left == (object)right) return true;
            if ((object)left == null || (object)right == null) return false;
            
            return left.Exchange == right.Exchange && left.Name == right.Name;
        }

        public static bool operator !=(Instrument left, Instrument right)
        {
            return !(left == right);
        }
    }
}
