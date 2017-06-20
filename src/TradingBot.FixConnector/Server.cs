using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Transport;
using TradingBot.Common.Infrastructure;
using TradingBot.FixConnector.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace TradingBot.FixConnector
{
    public class Server
    {
        private ILogger logger = Logging.CreateLogger<Server>();

        public async Task StartAsync(ConnectionConfig config, SessionSettings settings, CancellationToken token)
        {
            var queue = new Queue<FullOrderBook>();

            var application = new ICMConnector(config, queue);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new ScreenLogFactory(settings);


            using(var initiator = new SocketInitiator(application, storeFactory, settings, logFactory))
            {
                initiator.Start();

				while (!token.IsCancellationRequested)
				{
                    await Task.Delay(TimeSpan.FromSeconds(15), token);
                    logger.LogDebug($"{DateTime.Now} Server heartbeat");
				}

                initiator.Stop();
            }
        }
    }
}
