using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Moq;
using Lykke.ExternalExchangesApi.Shared;
using TradingBot.Infrastructure.WebSockets;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Infrastructure.WebSockets
{
    public class WebSocketSubscriberTests
    {
        private static readonly TimeSpan ReconnectionTime = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task SimpleCycleExecutesCorrectly()
        {
            // Setup

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new WebSocketSubscriber(messenger.Object, log);

            // Execute

            socket.Start();
            await Task.Delay(TimeSpan.FromMilliseconds(100)); // wait cycle to perform
            socket.Stop();
            socket.Dispose();

            // Check

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task SubscriberRestartsWhenNoMessagesReceived()
        {
            // Setup

            TimeSpan heartbeat = TimeSpan.FromSeconds(1);
            TimeSpan waitTime = TimeSpan.FromSeconds(1.5);
            TimeSpan executionTime = TimeSpan.FromSeconds(2);

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            messenger
                .Setup(m => m.GetResponseAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(async token =>
                {
                    await Task.Delay(executionTime, token); return ""; // Emulate no messages for a long time
                });

            var socket = new WebSocketSubscriber(messenger.Object, log, heartbeat);

            // Execute

            socket.Start();
            await Task.Delay(waitTime); // wait cycle and restart to perform
            socket.Stop();
            socket.Dispose();

            // Check

            // Heartbeat restart causes 2 times connect/stop
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task StopCallInterruptsConnect()
        {
            // Setup

            TimeSpan waitTime = TimeSpan.FromMilliseconds(100);
            TimeSpan controlTime = TimeSpan.FromMilliseconds(300);
            TimeSpan executionTime = TimeSpan.FromSeconds(10);

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            messenger
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(async token =>
                {
                    await Task.Delay(executionTime, token); // Emulate long connection
                });
            var socket = new WebSocketSubscriber(messenger.Object, log);

            // Execute

            Stopwatch watch = new Stopwatch();
            watch.Start();

            socket.Start();
            await Task.Delay(waitTime); // wait connection
            socket.Stop();
            socket.Dispose();

            watch.Stop();

            // Check

            // Connection must be interrupted
            Assert.True(watch.Elapsed < controlTime);

            // 1 call Connect/Stop
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task StopCallInterruptsGetResponse()
        {
            // Setup

            TimeSpan waitTime = TimeSpan.FromMilliseconds(100);
            TimeSpan controlTime = TimeSpan.FromMilliseconds(300);
            TimeSpan executionTime = TimeSpan.FromSeconds(10);
            var log = new LogToMemory();

            var messenger = CreateDefaultMockMessenger();
            messenger
                .Setup(m => m.GetResponseAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(async token =>
                {
                    await Task.Delay(executionTime, token); // Emulate long connection
                    return "";
                });
            var socket = new WebSocketSubscriber(messenger.Object, log);

            // Execute

            Stopwatch watch = new Stopwatch();
            watch.Start();

            socket.Start();
            await Task.Delay(waitTime); // wait connection
            socket.Stop();
            socket.Dispose();

            watch.Stop();

            // Check

            // Connection must be interrupted
            Assert.True(watch.Elapsed < controlTime);

            // 1 call Connect/Stop
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task HeartbeatDoesNotRestartAfterStop()
        {
            // Setup

            TimeSpan heartbeat = TimeSpan.FromMilliseconds(500);
            TimeSpan waitTime = TimeSpan.FromMilliseconds(1000);

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new WebSocketSubscriber(messenger.Object, log, heartbeat);

            // Execute

            socket.Start();
            await Task.Delay(TimeSpan.FromMilliseconds(50)); // give time to charge heartbeat
            socket.Stop();
            await Task.Delay(waitTime); // wait for possible heartbeat recharge 
            socket.Dispose();

            // Check

            // 1 call Connect/Stop
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task SubscriberCanBeRestartedMultipleTimes()
        {
            // Setup

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();

            var income = new List<string>();
            var socket = new WebSocketSubscriber(messenger.Object, log);
            socket.Subscribe(s => { income.Add(s); return Task.FromResult(0); });

            // Execute

            for (int i = 0; i < 10; i++)
            {
                messenger
                    .Setup(m => m.GetResponseAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(i.ToString()));

                socket.Start();
                await Task.Delay(50);
                socket.Stop();
            }
            socket.Dispose();

            // Check

            // Should receive all 'i': from 1 to 10
            Assert.Equal(45, income.GroupBy(i => i).Select(g => Int32.Parse(g.Key)).Sum());

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task MethodsStartStopAreIdempotent()
        {
            // Setup

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();

            var income = new List<string>();
            var socket = new WebSocketSubscriber(messenger.Object, log);
            socket.Subscribe(s => { income.Add(s); return Task.FromResult(0); });

            // Execute

            socket.Start();
            socket.Start();
            socket.Start();
            await Task.Delay(50);
            socket.Stop();
            socket.Stop();
            socket.Stop();
            socket.Dispose();
            socket.Dispose();
            socket.Dispose();

            // Check

            // Should receive all 'i': from 1 to 10
            Assert.True(income.Count > 0);

            // Expecting no exceptions and no errors
            Assert.True(log.NoErrors());
        }

        [Fact]
        public async Task SubscriberHandlesExceptionsOnConnection()
        {
            // Setup

            var count = 0;
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            messenger
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    if (count++ == 0)
                    {
                        throw new TimeoutException();
                    }
                }).Returns(Task.FromResult(0));
            var socket = new WebSocketSubscriber(messenger.Object, log);

            // Execute

            socket.Start();
            await Task.Delay(ReconnectionTime.Add(TimeSpan.FromMilliseconds(500))); // wait for connection retry
            socket.Stop();
            socket.Dispose();

            // Check

            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            // Expecting logged exception
            Assert.True(log.ContainsErrors());
        }

        [Fact]
        public async Task SubscriberHandlesExceptionsOnResponse()
        {
            // Setup
            //
            var count = 0;
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            messenger
                .Setup(m => m.GetResponseAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    if (count++ == 0)
                    {
                        throw new TimeoutException();
                    }
                }).Returns(Task.FromResult(""));
            var socket = new WebSocketSubscriber(messenger.Object, log);

            // Execute

            socket.Start();
            await Task.Delay(ReconnectionTime.Add(TimeSpan.FromMilliseconds(500))); // wait for connection retry
            socket.Stop();
            socket.Dispose();

            // Check

            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));

            // Expecting logged exception
            Assert.True(log.ContainsErrors());
        }

        [Fact]
        public async Task SubscriberHandlesExceptionsOnHandle()
        {
            // Setup

            var count = 0;
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new WebSocketSubscriber(messenger.Object, log);
            socket.Subscribe(s =>
            {
                if (count++ == 0)
                {
                    throw new InvalidOperationException();
                }
                return Task.FromResult(0);
            });

            // Execute

            socket.Start();
            await Task.Delay(100);
            socket.Stop();
            socket.Dispose();

            // Check

            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));

            Assert.True(count > 1); // Expecting messages to continue
            // Expecting logged exception
            Assert.True(log.ContainsErrors());
        }

        [Fact]
        public async Task SetupSubscriberToStopOnFailedConnection()
        {
            // Setup

            TimeSpan heartbeat = TimeSpan.FromMilliseconds(500);
            TimeSpan waitTime = TimeSpan.FromMilliseconds(1000);

            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new DerivedWebSocketSubscriber(messenger.Object, log, heartbeat)
            {
                ConnectCallback = (token) => Task.FromException(new AuthenticationException())
            };

            // Execute

            socket.Start();
            await Task.Delay(waitTime); // wait for possible heartbeat recharge 
            socket.Dispose();

            // Check

            // 1 call Connect/Stop. No retries.
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.Never);
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task SetupSubscriberToContinueOnFailedConnection()
        {
            // Setup

            var count = 0;
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new DerivedWebSocketSubscriber(messenger.Object, log)
            {
                ConnectCallback = (token) =>
                {
                    var failure = count++ == 0;
                    if (failure)
                    {
                        throw new InvalidOperationException();
                    }
                    return Task.CompletedTask;
                }
            };

            // Execute
            //
            socket.Start();
            await Task.Delay(ReconnectionTime.Add(TimeSpan.FromMilliseconds(100))); // wait for connection retry
            socket.Dispose();

            // Check
            //

            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public void SetupSubscriberToStopOnFailedHandle()
        {
            // Setup

            TimeSpan heartbeat = Timeout.InfiniteTimeSpan;
            var lk = new ManualResetEventSlim(false);
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var socket = new DerivedWebSocketSubscriber(messenger.Object, log, heartbeat)
            {
                HandleCallback = (token) =>
                {
                    lk.Set();
                    return Task.FromException(new AuthenticationException());
                }
            };

            // Execute

            socket.Start();
            Thread.Sleep(10000);
            socket.Dispose();

            // Check

            // 1 call Connect/Stop. No retries.
            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task SetupSubscriberToContinueOnFailedHandle()
        {
            // Setup

            var count = 0;
            var log = new LogToMemory();
            var messenger = CreateDefaultMockMessenger();
            var _lock = new ManualResetEvent(false);
            var socket = new DerivedWebSocketSubscriber(messenger.Object, log)
            {
                HandleCallback = (token) =>
                {
                    if (++count < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    _lock.Set();
                    return Task.CompletedTask;
                }
            };

            // Execute

            socket.Start();
            _lock.WaitOne();
            socket.Dispose();
            // Check

            messenger.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            messenger.Verify(m => m.GetResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            messenger.Verify(m => m.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private static Mock<IMessenger<object, string>> CreateDefaultMockMessenger()
        {
            var messenger = new Mock<IMessenger<object, string>>();
            messenger
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));

            messenger
                .Setup(m => m.GetResponseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(""));

            messenger
                .Setup(m => m.StopAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));

            return messenger;
        }
    }

    /// <summary>
    /// Derived WebSocketSubscriber to test failure/continue cases
    /// </summary>
    class DerivedWebSocketSubscriber : WebSocketSubscriber
    {
        public Func<CancellationToken, Task> ConnectCallback { get; set; } = token => Task.CompletedTask;
        public Func<CancellationToken, Task> HandleCallback { get; set; } = token => Task.CompletedTask;

        public DerivedWebSocketSubscriber(
            IMessenger<object, string> messenger,
            ILog log,
            TimeSpan? heartbeatPeriod = null)
            : base(messenger, log, heartbeatPeriod)
        {
        }

        protected override async Task Connect(CancellationToken token)
        {
            await base.Connect(token);
            await ConnectCallback(token);
        }

        protected override async Task HandleResponse(string json, CancellationToken token)
        {
            await base.HandleResponse(json, token);
            await HandleCallback(token);

        }
    }


    class LogEntity
    {
        public string DateTime { get; set; }
        public string Level { get; set; }
        public string Component { get; set; }
        public string Process { get; set; }
        public string Context { get; set; }
        public string Type { get; set; }
        public string Msg { get; set; }

        public static readonly string InfoLevel = "info";
        public static readonly string WarningLevel = "warning";
        public static readonly string ErrorLevel = "error";
    }

    static class LogExtensions
    {
        public static List<LogEntity> ToTypedList(this LogToMemory log)
        {
            var headers = log.TableData.Headers.ToList();

            return log.TableData.Data
                .Select(i => new LogEntity()
                {
                    DateTime = i[headers.IndexOf("Date Time")],
                    Level = i[headers.IndexOf("Level")],
                    Component = i[headers.IndexOf("Component")],
                    Process = i[headers.IndexOf("Process")],
                    Context = i[headers.IndexOf("Context")],
                    Type = i[headers.IndexOf("Type")],
                    Msg = i[headers.IndexOf("Msg")]
                })
                .ToList();
        }

        public static bool NoErrors(this LogToMemory log)
        {
            return log
                .ToTypedList()
                .All(e => e.Level == LogEntity.InfoLevel || e.Level == LogEntity.WarningLevel);
        }

        public static bool ContainsErrors(this LogToMemory log)
        {
            return log
                .ToTypedList()
                .Any(e => e.Level == LogEntity.ErrorLevel);
        }
    }
}
