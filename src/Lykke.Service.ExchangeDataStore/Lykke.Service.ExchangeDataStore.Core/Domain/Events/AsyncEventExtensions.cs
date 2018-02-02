using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.Events
{
    public static class AsyncEventExtensions
    {
        public static async Task NullableInvokeAsync<T>(this AsyncEvent<T> asyncEvent,
            object sender, T eventArgs)
        {
            if (asyncEvent != null)
                await asyncEvent.InvokeAsync(sender, eventArgs);
        }        
    }
}
