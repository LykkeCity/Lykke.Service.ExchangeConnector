using System.Collections.Generic;
using System.Linq;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Models.Api;

namespace TradingBot.Infrastructure.Monitoring
{
    internal sealed class ExchangeRatingValuer : IExchangeRatingValuer
    {
        private readonly ExchangeStatisticsCollector _statisticsCollector;
        private readonly IReadOnlyCollection<Exchange> _exchanges;
        private int _minResponseTime = int.MaxValue;
        private const double ExceptionWeight = 0.5d;
        private const double SpeedWeight = 1d - ExceptionWeight;


        public ExchangeRatingValuer(ExchangeStatisticsCollector statisticsCollector, IReadOnlyCollection<Exchange> exchanges)
        {
            _statisticsCollector = statisticsCollector;
            _exchanges = exchanges;
        }


        public IReadOnlyCollection<ExchangeRatingModel> Rating
        {
            get
            {
                FindMinResponseTime();
                var cr = _statisticsCollector.GetCallStatistics().Select(cs => cs.ExchangeName)
                    .Union(_statisticsCollector.GetExceptionStatistics().Select(cs => cs.ExchangeName)).
                    GroupBy(k => k).Select(st => CalculateRating(st.Key));

                // left outer join for setting initial values
                var result = from e in _exchanges
                             join em in cr on e.Name equals em.ExchangeName into g
                             from e2 in g.DefaultIfEmpty(new ExchangeRatingModel { ExchangeName = e.Name, Rating = e.Config.InitialRating })
                             select e2;
                return result.ToArray();
            }
        }

        private ExchangeRatingModel CalculateRating(string exchangeName)
        {
            var callStatistic = _statisticsCollector.GetCallStatistics().Where(es => es.ExchangeName == exchangeName).ToArray();

            var exCount = _statisticsCollector.GetExceptionStatistics().Count(es => es.ExchangeName == exchangeName);
            var exCoef = ExceptionWeight * callStatistic.Length / (exCount + callStatistic.Length);

            var avgResponse = callStatistic.Select(cs => cs.Duration.Milliseconds)
                .DefaultIfEmpty(_minResponseTime)
                .Average(cs => cs);

            var callCoef = SpeedWeight * _minResponseTime / avgResponse;

            var rating = (exCoef + callCoef) * 10d;
            return new ExchangeRatingModel
            {
                ExchangeName = exchangeName,
                Rating = rating
            };
        }


        private void FindMinResponseTime()
        {
            _minResponseTime = _statisticsCollector.GetCallStatistics()
                .Select(cs => cs.Duration.Milliseconds)
                .DefaultIfEmpty(_minResponseTime)
                .Min(cs => cs);
        }
    }
}
