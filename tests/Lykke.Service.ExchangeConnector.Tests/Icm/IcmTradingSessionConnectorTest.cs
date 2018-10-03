using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient;
using Lykke.ExternalExchangesApi.Shared;
using Microsoft.Extensions.Logging;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using TradingBot.Infrastructure.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Lykke.Service.ExchangeConnector.Tests.Icm
{
    public sealed class IcmTradingSessionConnectorTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private const string TargetIp = "";
        private const int TargetPort = 80;

        private const string OrderSenderCompId = "";
        private const string OrderTargetCompId = "";
        private const string Password = "";

        private readonly IcmTradeSessionConnector _connector;

        public IcmTradingSessionConnectorTest(ITestOutputHelper output)
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
                    "DataDictionary=FIX44.xml",
                    "HeartBtInt=15",
                    "SSLEnable=N",
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
            var connectorConfig = new FixConnectorConfiguration(config.Password, config.GetTradingFixConfigAsReader());
            _connector = new IcmTradeSessionConnector(connectorConfig, new TestOutput(new TestOutputHelperWrapper(_output)));
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
                    "DataDictionary=FIX44.xml",
                    "HeartBtInt=15",
                    $"SocketConnectHost={TargetIp}",
                    $"SocketConnectPort={TargetPort}",
                    "[SESSION]",
                    $"SenderCompID={OrderSenderCompId}",
                    $"TargetCompID={OrderTargetCompId}",
                    "StartTime=05:00:00",
                    "EndTime=23:00:00"
                }
            };
            var connectorConfig = new FixConnectorConfiguration(config.Password, config.GetTradingFixConfigAsReader());
            var connector = new IcmTradeSessionConnector(connectorConfig, new TestOutput(new TestOutputHelperWrapper(_output)));
            connector.Start();

            WaitForState(FixConnectorState.Connected, 5);
        }

        [Fact]
        public void TestLogout()
        {
            _connector.Start();

            WaitForState(FixConnectorState.Connected, 10);

            _connector.Stop();

            WaitForState(FixConnectorState.Disconnected, 10);

        }

        [Fact]
        public async Task ShouldGetPositions()
        {
            _connector.Start();
            WaitForState(FixConnectorState.Connected, 30);

            var request = new RequestForPositions
            {
                PosReqID = new PosReqID(nameof(RequestForPositions) + Guid.NewGuid()),
                PosReqType = new PosReqType(PosReqType.POSITIONS),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT),
                NoPartyIDs = new NoPartyIDs(1),
                Account = new Account("account"),
                AccountType = new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS),
                ClearingBusinessDate = new ClearingBusinessDate(DateTimeConverter.ConvertDateOnly(DateTime.UtcNow.Date)),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };

            var partyGroup = new RequestForPositions.NoPartyIDsGroup
            {
                PartyID = new PartyID("FB"),
                PartyRole = new PartyRole(PartyRole.CLIENT_ID)
            };

            request.AddGroup(partyGroup);

            var resp = await _connector.GetPositionsAsync(request, CancellationToken.None);

            Assert.NotEmpty(resp);
        }

        [Fact]
        public async Task ShouldCreateOrder()
        {
            _connector.Start();
            WaitForState(FixConnectorState.Connected, 30);

            var newOrderSingle = new NewOrderSingle
            {
                Symbol = new Symbol(@"EUR/USDm"),
               Currency = new Currency("EUR"),
                Side = new Side(Side.SELL),
                OrderQty = new OrderQty(1000),
                OrdType = new OrdType(OrdType.MARKET),
                TimeInForce = new TimeInForce(TimeInForce.GOOD_TILL_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow),
                Price = new Price(1.44m)
            };

            var resp = await _connector.AddOrderAsync(newOrderSingle, CancellationToken.None);
            Assert.NotEmpty(resp);


        }

        [Fact]
        public async Task ShouldCreateDifferentOrders()
        {
            _connector.Start();
            WaitForState(FixConnectorState.Connected, 30);
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
                    catch (Exception)
                    {
                        // OK
                    }
                }

            }


        }

        [Fact]
        public async Task ShouldSendHeartBeat()
        {
            _connector.Start();
            WaitForState(FixConnectorState.Connected, 30);

            await Task.Delay(TimeSpan.FromMinutes(10));

        }


        private void WaitForState(FixConnectorState state, int timeout)
        {
            for (var i = 0; i < timeout; i++)
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
            WaitForState(FixConnectorState.Disconnected, 10);
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
#pragma warning disable S4144 // Methods should not have identical implementations

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) where TState : LogEntryParameters
            {
                
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope(string scopeMessage)
            {
                throw new NotImplementedException();
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
#pragma warning restore S4144 // Methods should not have identical implementations

        }
    }
}
