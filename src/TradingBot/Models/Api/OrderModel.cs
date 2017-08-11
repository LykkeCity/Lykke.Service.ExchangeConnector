using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TradingBot.Trading;

namespace TradingBot.Models.Api
{
    public class OrderModel : ISignedModel//, IValidatableObject
    {
        public string ExchangeName { get; set; }
        
        [Required]
        public string Instrument { get; set; }
        
        [Required]
        public TradeType TradeType { get; set; }
        
        [Required]
        public OrderType OrderType { get; set; }
        
        [Required]
        [Range(0.0000, Double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(0.0001, Double.MaxValue)]
        public decimal Volume { get; set; }
        
        [Required]
        [Range(1, long.MaxValue)]
        public long Id { get; set; }
        
        public DateTime DateTime { get; set; }
        
        public string GetStringToSign()
        {
            return $"{DateTime:s}{Id}{Instrument}{TradeType}{OrderType}{Price:0.0000}{Volume:0.0000}";
        }

//        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
//        {
//            throw new NotImplementedException();
//        }
    }
}