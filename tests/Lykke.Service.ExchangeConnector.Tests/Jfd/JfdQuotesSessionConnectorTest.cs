using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Jfd;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Exchanges.Concrete.Jfd.FixClient;
using TradingBot.Infrastructure.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Lykke.Service.ExchangeConnector.Tests.Jfd
{
    public sealed class JfdQuotesSessionConnectorTest : IDisposable
    {
        private readonly ILog _output;
        private const string TargetIp = "";
        private const int TargetPort = 80;

        private const string QuoteSenderCompId = "";
        private const string QuoteTargetCompId = "";
        private const string Password = "";

        private readonly JfdQuotesSessionConnector _connector;

        public JfdQuotesSessionConnectorTest(ITestOutputHelper output)
        {
            _output = new TestOutput(output);
            var config = new JfdExchangeConfiguration()
            {
                Password = Password,
                QuotingFixConfiguration = new[]
                {
                    "[DEFAULT]",
                    "ResetOnLogon=Y",
                    "FileStorePath=store",
                    "FileLogPath=log",
                    "ConnectionType=initiator",
                    "ReconnectInterval=60",
                    "BeginString=FIX.4.4",
                    "DataDictionary=FIX44.jfd.xml",
                    "HeartBtInt=15",
                    "SSLEnable=Y",
                    "SSLProtocols=Tls",
                    "SSLValidateCertificates=N",
                    $"SocketConnectHost={TargetIp}",
                    $"SocketConnectPort={TargetPort}",
                    "[SESSION]",
                    $"SenderCompID={QuoteSenderCompId}",
                    $"TargetCompID={QuoteTargetCompId}",
                    "StartTime=05:00:00",
                    "EndTime=23:00:00"
                }
            };
            var jfdConfig = new JfdConnectorConfiguration(config.Password, config.GetQuotingFixConfigAsReader());
            _connector = new JfdQuotesSessionConnector(jfdConfig, _output);
        }


        [Fact]
        public async Task ShouldReceiveOrderBooks()
        {
            await _connector.ConnectAsync(CancellationToken.None);
            WaitForState(JfdConnectorState.Connected, 30);
            var symbols = new[] { "USDCHF", "EURUSD" };
            var request = new MarketDataRequest()
            {
                MDReqID = new MDReqID(DateTime.UtcNow.Ticks.ToString()),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                MarketDepth = new MarketDepth(50),
                MDUpdateType = new MDUpdateType(MDUpdateType.FULL_REFRESH),
                NoMDEntryTypes = new NoMDEntryTypes(2),
                NoRelatedSym = new NoRelatedSym(symbols.Length)
            };
            var bidGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.BID)
            };
            var askGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.OFFER)
            };
            foreach (var symb in symbols)
            {
                var noRelatedSymGroup = new MarketDataRequest.NoRelatedSymGroup
                {
                    Symbol = new Symbol(symb)
                };
                request.AddGroup(noRelatedSymGroup);
            }

            request.AddGroup(bidGroup);
            request.AddGroup(askGroup);

            await _connector.SendRequestAsync(request, CancellationToken.None);
            for (int i = 0; i < symbols.Length; i++)
            {
                var resp = await _connector.GetResponseAsync(CancellationToken.None);

                if (resp is MarketDataSnapshotFullRefresh snapshot)
                {
                    var symbol = snapshot.Symbol.Obj;
                    for (int j = 1; j <= snapshot.NoMDEntries.Obj; j++)
                    {
                        var ob = resp.GetGroup(j, new MarketDataSnapshotFullRefresh.NoMDEntriesGroup());
                        var dir = ob.GetField(new MDEntryType()).Obj;
                        var price = ob.GetField(new MDEntryPx()).Obj;
                        var size = ob.GetField(new MDEntrySize()).Obj;

                        await _output.WriteInfoAsync("", "", $"Sym:{symbol} side:{dir} price:{price} size:{size}");
                    }
                }


            }

        }    
        
        
        [Fact]
        public async Task ShouldReceiveOrderBooksForLongTime()
        {
            await _connector.ConnectAsync(CancellationToken.None);
            WaitForState(JfdConnectorState.Connected, 30);
            var symbols = new[] { "USDCHF", "EURUSD" };
            var request = new MarketDataRequest()
            {
                MDReqID = new MDReqID(DateTime.UtcNow.Ticks.ToString()),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                MarketDepth = new MarketDepth(50),
                MDUpdateType = new MDUpdateType(MDUpdateType.FULL_REFRESH),
                NoMDEntryTypes = new NoMDEntryTypes(2),
                NoRelatedSym = new NoRelatedSym(symbols.Length)
            };
            var bidGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.BID)
            };
            var askGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.OFFER)
            };
            foreach (var symb in symbols)
            {
                var noRelatedSymGroup = new MarketDataRequest.NoRelatedSymGroup
                {
                    Symbol = new Symbol(symb)
                };
                request.AddGroup(noRelatedSymGroup);
            }

            request.AddGroup(bidGroup);
            request.AddGroup(askGroup);

            await _connector.SendRequestAsync(request, CancellationToken.None);
            for (int i = 0; i < 1000; i++)
            {
                var resp = await _connector.GetResponseAsync(CancellationToken.None);

                if (resp is MarketDataSnapshotFullRefresh snapshot)
                {
                    var symbol = snapshot.Symbol.Obj;
                    for (int j = 1; j <= snapshot.NoMDEntries.Obj; j++)
                    {
                        var ob = resp.GetGroup(j, new MarketDataSnapshotFullRefresh.NoMDEntriesGroup());
                        var dir = ob.GetField(new MDEntryType()).Obj;
                        var price = ob.GetField(new MDEntryPx()).Obj;
                        var size = ob.GetField(new MDEntrySize()).Obj;

                        await _output.WriteInfoAsync("", "", $"Sym:{symbol} side:{dir} price:{price} size:{size}");
                    }
                }


            }

        }

        [Fact]
        public async Task RequestInvalidSymbol()
        {
            await _connector.ConnectAsync(CancellationToken.None);
            WaitForState(JfdConnectorState.Connected, 30);
            var symbols = new[] { "AAABBB" };
            var request = new MarketDataRequest()
            {
                MDReqID = new MDReqID(DateTime.UtcNow.Ticks.ToString()),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                MarketDepth = new MarketDepth(50),
                MDUpdateType = new MDUpdateType(MDUpdateType.FULL_REFRESH),
                NoMDEntryTypes = new NoMDEntryTypes(2),
                NoRelatedSym = new NoRelatedSym(symbols.Length)
            };
            var bidGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.BID)
            };
            var askGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.OFFER)
            };
            foreach (var symb in symbols)
            {
                var noRelatedSymGroup = new MarketDataRequest.NoRelatedSymGroup
                {
                    Symbol = new Symbol(symb)
                };
                request.AddGroup(noRelatedSymGroup);
            }

            request.AddGroup(bidGroup);
            request.AddGroup(askGroup);

            await _connector.SendRequestAsync(request, CancellationToken.None);
            for (int i = 0; i < 2; i++)
            {
                var resp = await _connector.GetResponseAsync(CancellationToken.None);

                if (resp is MarketDataSnapshotFullRefresh snapshot)
                {
                    var symbol = snapshot.Symbol.Obj;
                    for (int j = 1; j <= snapshot.NoMDEntries.Obj; j++)
                    {
                        var ob = resp.GetGroup(j, new MarketDataSnapshotFullRefresh.NoMDEntriesGroup());
                        var dir = ob.GetField(new MDEntryType()).Obj;
                        var price = ob.GetField(new MDEntryPx()).Obj;
                        var size = ob.GetField(new MDEntrySize()).Obj;

                        await _output.WriteInfoAsync($"Sym:{symbol} side:{dir} price:{price} size:{size}", "", "");
                    }
                }


            }

        }



        private void WaitForState(JfdConnectorState state, int timeout)
        {
            for (int i = 0; i < timeout; i++)
            {
                Thread.Sleep(1000);
                if (_connector.State == state)
                {
                    break;
                }
            }

            Assert.Equal(state, _connector.State);
        }

        public void Dispose()
        {
            _connector.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            WaitForState(JfdConnectorState.Disconnected, 30);
            _connector?.Dispose();
        }


        private class TestOutputHelperWrapper : ITestOutputHelper
        {
            private readonly ITestOutputHelper _output;

            public TestOutputHelperWrapper(ITestOutputHelper output)
            {
                _output = output;
            }
            public void WriteLine(string message)
            {
                _output.WriteLine(message.Replace('', '|'));
                Console.WriteLine(message.Replace('', '|'));
            }

            public void WriteLine(string format, params object[] args)
            {
                throw new NotImplementedException();
            }
        }
        private class TestOutput : ILog
        {
            private readonly ITestOutputHelper _underlying;

            public TestOutput(ITestOutputHelper underlying)
            {
                _underlying = new TestOutputHelperWrapper(underlying);
            }
            public Task WriteInfoAsync(string component, string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteMonitorAsync(string component, string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteWarningAsync(string component, string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteWarningAsync(string component, string process, string context, string info, Exception ex, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteErrorAsync(string component, string process, string context, Exception exception, DateTime? dateTime = null)
            {
                _underlying.WriteLine(exception.ToString());
                return Task.CompletedTask;
            }

            public Task WriteFatalErrorAsync(string component, string process, string context, Exception exception, DateTime? dateTime = null)
            {
                _underlying.WriteLine(exception.ToString());
                return Task.CompletedTask;
            }

            public Task WriteInfoAsync(string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteMonitorAsync(string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteWarningAsync(string process, string context, string info, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteWarningAsync(string process, string context, string info, Exception ex, DateTime? dateTime = null)
            {
                _underlying.WriteLine(info);
                return Task.CompletedTask;
            }

            public Task WriteErrorAsync(string process, string context, Exception exception, DateTime? dateTime = null)
            {
                _underlying.WriteLine(exception.ToString());
                return Task.CompletedTask;
            }

            public Task WriteFatalErrorAsync(string process, string context, Exception exception, DateTime? dateTime = null)
            {
                _underlying.WriteLine(exception.ToString());
                return Task.CompletedTask;
            }
        }
    }
}
