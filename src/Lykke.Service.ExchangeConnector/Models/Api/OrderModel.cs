using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using TradingBot.Trading;

namespace TradingBot.Models.Api
{
    public class OrderModel 
    {
        [JsonProperty(Required = Required.Always)]
        public string ExchangeName { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Instrument { get; set; }

        [Required]
        public TradeType TradeType { get; set; }

        [Required]
        public OrderType OrderType { get; set; }

        [DefaultValue(TimeInForce.FillOrKill)]
        public TimeInForce TimeInForce { get; set; }

        [Range(0.0, double.MaxValue)]
        public decimal? Price { get; set; }

        [JsonProperty(Required = Required.Always)]
        [Range(0.0001, double.MaxValue)]
        public decimal Volume { get; set; }

        /// <summary>
        /// Date and time must be in 5 minutes threshold from UTC now
        /// </summary>
        public DateTime DateTime { get; set; }


    }
}
