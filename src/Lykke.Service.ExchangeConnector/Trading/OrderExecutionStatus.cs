using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Trading
{
    public enum OrderExecutionStatus
    {
        Unknown,
        Fill,
        PartialFill,
        Cancelled,
        Rejected,
        New,
        Pending
    }
}
