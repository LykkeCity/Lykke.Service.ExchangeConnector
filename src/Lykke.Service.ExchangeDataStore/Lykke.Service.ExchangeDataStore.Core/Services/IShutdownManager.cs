using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}