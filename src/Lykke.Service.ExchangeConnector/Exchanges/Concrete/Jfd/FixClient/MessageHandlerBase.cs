using System.Collections.Generic;
using Common.Log;
using ILog = Common.Log.ILog;
using Message = QuickFix.Message;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal abstract class MessageHandlerBase<T> : IMessageHandler
    {
        protected readonly Dictionary<string, T> Requests = new Dictionary<string, T>();

        public abstract bool HandleMessage(Message message);

        protected readonly ILog Log;

        protected MessageHandlerBase(ILog log)
        {
            Log = log.CreateComponentScope(GetType().Name);
        }

        public void RejectMessage(string id)
        {
            Requests.Remove(id);
        }
    }
}
