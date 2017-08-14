using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Communications
{
    public class FixMessageTableEntity : TableEntity
    {   
        public int DirectionInt { get; set; }

        [IgnoreProperty]
        public FixMessageDirection Direction
        {
            get => (FixMessageDirection) DirectionInt;
            set => DirectionInt = (int) value;
        }
        
        public string Message { get; set; }
        
        public string Type { get; set; }
    }

    public enum FixMessageDirection
    {
        FromApp = 1,
        FromAdmin = 2,
        ToApp = 3,
        ToAdmin = 4
    }
}