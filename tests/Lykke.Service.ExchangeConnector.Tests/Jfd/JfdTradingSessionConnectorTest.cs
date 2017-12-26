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
    public sealed class JfdTradingSessionConnectorTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private const string TargetIp = "";
        private const int TargetPort = 80;

        private const string OrderSenderCompId = "";
        private const string OrderTargetCompId = "";
        private const string Password = "";

        private readonly JfdTradeSessionConnector _connector;

        public JfdTradingSessionConnectorTest(ITestOutputHelper output)
        {
            _output = output;
            var config = new JfdExchangeConfiguration()
            {
                Password = Password,
                TradingFixConfiguration = new[]
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
                    $"SenderCompID={OrderSenderCompId}",
                    $"TargetCompID={OrderTargetCompId}",
                    "StartTime=05:00:00",
                    "EndTime=23:00:00"
                }
            };
            var connectorConfig = new JfdConnectorConfiguration(config.Password, config.GetTradingFixConfigAsReader());
            _connector = new JfdTradeSessionConnector(connectorConfig, new TestOutput(new TestOutputHelperWrapper(_output)));
        }

        [Fact]
        public void TestLogon()
        {
            _connector.Start();

            WaitForState(JfdConnectorState.Connected, 600);

            Thread.Sleep(1000000);
        }

        [Fact]
        public void TestLogonWithInvalidPassword()
        {
            var config = new JfdExchangeConfiguration()
            {
                Password = "InvalidPassword",
                TradingFixConfiguration = new[]
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
                    $"SenderCompID={OrderSenderCompId}",
                    $"TargetCompID={OrderTargetCompId}",
                    "StartTime=05:00:00",
                    "EndTime=23:00:00"
                }
            };
            var connectorConfig = new JfdConnectorConfiguration(config.Password, config.GetTradingFixConfigAsReader());
            var connector = new JfdTradeSessionConnector(connectorConfig, new TestOutput(new TestOutputHelperWrapper(_output)));
            connector.Start();

            WaitForState(JfdConnectorState.Connected, 5);
        }

        [Fact]
        public void TestLogout()
        {
            _connector.Start();

            WaitForState(JfdConnectorState.Connected, 10);

            _connector.Stop();

            WaitForState(JfdConnectorState.Disconnected, 10);

        }

        [Fact]
        public async Task ShouldGetPositions()
        {
            _connector.Start();
            WaitForState(JfdConnectorState.Connected, 30);

            var pr = new RequestForPositions()
            {
                PosReqType = new PosReqType(PosReqType.POSITIONS),
                NoPartyIDs = new NoPartyIDs(1),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };
            var partyGroup = new RequestForPositions.NoPartyIDsGroup
            {
                PartyID = new PartyID("8")
            };
            pr.AddGroup(partyGroup);

            var resp = await _connector.GetPositionsAsync(pr, CancellationToken.None);

            Assert.NotEmpty(resp);
        }

        [Fact]
        public async Task ShouldCreateOrder()
        {
            _connector.Start();
            WaitForState(JfdConnectorState.Connected, 30);

            var newOrderSingle = new NewOrderSingle
            {
                //  NoPartyIDs = new NoPartyIDs(0),
                HandlInst = new HandlInst(HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE),
                Symbol = new Symbol("EURUSD"),
                Side = new Side(Side.BUY),
                OrderQty = new OrderQty(100),
                OrdType = new OrdType(OrdType.LIMIT),
                TimeInForce = new TimeInForce(TimeInForce.IMMEDIATE_OR_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow),
                Price = new Price(42)
            };

            var resp = await _connector.AddOrderAsync(newOrderSingle, CancellationToken.None);
            Assert.NotEmpty(resp);


        }

        [Fact]
        public async Task ShouldCreateDifferentOrders()
        {
            _connector.Start();
            WaitForState(JfdConnectorState.Connected, 30);
            var allTiF = new[] { TimeInForce.IMMEDIATE_OR_CANCEL, TimeInForce.GOOD_TILL_CANCEL, TimeInForce.FILL_OR_KILL };
            var allOrdTypes = new[] { OrdType.MARKET };

            var counter = 1;
            foreach (var tif in allTiF)
            {
                foreach (var ordType in allOrdTypes)
                {
                    var newOrderSingle = new NewOrderSingle
                    {
                        //  NoPartyIDs = new NoPartyIDs(0),
                        HandlInst = new HandlInst(HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE),
                        Symbol = counter % 2 == 0 ? new Symbol("EURUSD") : new Symbol("USDCHF"),
                        Side = counter % 3 == 0 ? new Side(Side.BUY) : new Side(Side.SELL),
                        OrderQty = new OrderQty(counter),
                        OrdType = new OrdType(ordType),
                        TimeInForce = new TimeInForce(tif),
                        //      Price = new Price(counter * 10),
                        TransactTime = new TransactTime(DateTime.UtcNow)
                    };
                    counter++;
                    try
                    {
                        await _connector.AddOrderAsync(newOrderSingle, CancellationToken.None);
                    }
                    catch (Exception e)
                    {

                    }
                }

            }


        }

        [Fact]
        public async Task ShouldGetCollateral()
        {
            _connector.Start();
            WaitForState(JfdConnectorState.Connected, 30);

            var pr = new CollateralInquiry()
            {
                NoPartyIDs = new NoPartyIDs(1)
            };

            var partyGroup = new CollateralInquiry.NoPartyIDsGroup
            {
                PartyID = new PartyID("*")
            };
            pr.AddGroup(partyGroup);


            var resp = await _connector.GetCollateralAsync(pr, CancellationToken.None);
            Assert.NotEmpty(resp);


        }

        [Fact]
        public async Task ShouldSendHeartBeat()
        {
            _connector.Start();
            WaitForState(JfdConnectorState.Connected, 30);

            await Task.Delay(TimeSpan.FromMinutes(10));

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
            _connector.Stop();
            WaitForState(JfdConnectorState.Disconnected, 10);
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
                _underlying = underlying;
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
