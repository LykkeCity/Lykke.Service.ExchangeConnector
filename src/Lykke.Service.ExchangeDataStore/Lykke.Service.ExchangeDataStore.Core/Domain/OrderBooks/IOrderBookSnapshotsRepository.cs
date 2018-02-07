using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public interface IOrderBookSnapshotsRepository
    {
        Task SaveAsync(IOrderBookSnapshot orderBook, CancellationToken cancelToken);
    }
}
