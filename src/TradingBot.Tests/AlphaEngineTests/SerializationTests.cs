using System;
using Newtonsoft.Json;
using TradingBot.Common.Trading;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class SerializationTests
    {
        [Fact]
        public void DeserialiseTest()
        {
            var str = "[{\"Time\":\"2017-06-06T12:55:18Z\",\"Ask\":2893.99900,\"Bid\":2889.01100,\"Mid\":2891.50500},{\"Time\":\"2017-06-06T12:55:28Z\",\"Ask\":2894.00000,\"Bid\":2889.01100,\"Mid\":2891.50550},{\"Time\":\"2017-06-06T12:55:29Z\",\"Ask\":2894.00000,\"Bid\":2893.99900,\"Mid\":2893.99950}]";
            var obj = JsonConvert.DeserializeObject<TickPrice[]>(str); 

            Assert.NotNull(obj);
            Assert.Equal(3, obj.Length);
        }
    }
}
