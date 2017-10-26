using System.Collections.Generic;
using TradingBot.Models.Api;

namespace TradingBot.Infrastructure.Monitoring
{
    public interface IExchangeRatingValuer
    {
        IReadOnlyCollection<ExchangeRatingModel> Rating { get; }
    }
}
