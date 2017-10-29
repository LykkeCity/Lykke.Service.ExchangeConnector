using TradingBot.Exchanges.Abstractions;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
{
    public class Orders : BaseApi
    {
        public Orders(ApiClient apiClient) : base(apiClient)
        {
        }
    }
}
