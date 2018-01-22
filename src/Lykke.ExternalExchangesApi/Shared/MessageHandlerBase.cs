using System.Collections.Generic;
using Common.Log;
using ILog = Common.Log.ILog;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Shared
{
    internal abstract class MessageHandlerBase<T> : IMessageHandler
    {
        protected string ExchangeName { get; }
        protected readonly Dictionary<string, T> Requests = new Dictionary<string, T>();

        public abstract bool HandleMessage(Message message);

        protected readonly ILog Log;

        protected MessageHandlerBase(ILog log, string exchangeName)
        {
            ExchangeName = exchangeName;
            Log = log.CreateComponentScope(GetType().Name);
        }

        public void RejectMessage(string id)
        {
            Requests.Remove(id);
        }
    }
}
