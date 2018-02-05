using Common.Log;
using Lykke.Service.ExchangeDataStore.Core.Services;
using System.Threading.Tasks;
// ReSharper disable ClassNeverInstantiated.Global

namespace Lykke.Service.ExchangeDataStore.Services
{
    // NOTE: Sometimes, shutdown process should be expressed explicitly. 
    // If this is your case, use this class to manage shutdown.
    // For example, sometimes some state should be saved only after all incoming message processing and 
    // all periodical handler was stopped, and so on.

    public class ShutdownManager : IShutdownManager
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ILog _log;

        public ShutdownManager(ILog log)
        {
            _log = log;
        }

        public async Task StopAsync()
        {
            // TODO: Implement your shutdown logic here. Good idea is to log every step

            await Task.CompletedTask;
        }
    }
}
