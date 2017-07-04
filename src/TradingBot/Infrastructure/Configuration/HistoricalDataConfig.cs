using System;

namespace TradingBot.Infrastructure.Configuration
{
    public class HistoricalDataConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string BaseDirectory { get; set; }
        
        public string FileName { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string[] Instruments { get; set; }
    }
}