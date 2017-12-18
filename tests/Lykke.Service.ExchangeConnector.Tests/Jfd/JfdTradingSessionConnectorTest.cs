using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Jfd;
using QuickFix.Fields;
using QuickFix.FIX44;
using Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient;
using TradingBot.Infrastructure.Configuration;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Jfd
{
    public sealed class JfdTradingSessionConnectorTest : IDisposable
    {
        private const string TargetIp = "";
        private const int TargetPort = 80;

        private const string OrderSenderCompId = "";
        private const string OrderTargetCompId = "";
        private const string Password = "";

        private readonly JfdTradeSessionConnector _connector;

        public JfdTradingSessionConnectorTest()
        {
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
            _connector = new JfdTradeSessionConnector(connectorConfig, new LogToConsole());
        }

        [Fact]
        public void TestLogon()
        {
            _connector.Start();

            WaitForState(JfdConnectorState.Connected, 30);
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
                OrdType = new OrdType(OrdType.MARKET),
                TimeInForce = new TimeInForce(TimeInForce.IMMEDIATE_OR_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };

            var resp = await _connector.AddOrderAsync(newOrderSingle, CancellationToken.None);
            Assert.NotEmpty(resp);


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

        [Fact(Skip = "For manual run only")]
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
    }
}
