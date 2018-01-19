using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal sealed class TradingSignalsHandler : IStartable, IStopable
    {
        private readonly IReadOnlyDictionary<string, Exchange> exchanges;
        private readonly ILog logger;
        private readonly IHandler<ExecutionReport> _acknowledHandler;
        private readonly IHandler<ExecutionReport> _tradeHandler;
        private readonly TranslatedSignalsRepository translatedSignalsRepository;
        private readonly TimeSpan tradingSignalsThreshold = TimeSpan.FromMinutes(5);
        private readonly TimeSpan apiTimeout;
        private readonly RabbitMqSubscriber<TradingSignal> _messageProducer;
        private readonly bool _enabled;

        public TradingSignalsHandler(IEnumerable<Exchange> exchanges, ILog logger, 
            IHandler<ExecutionReport> acknowledHandler, 
            IHandler<ExecutionReport> tradeHandler, 
            TranslatedSignalsRepository translatedSignalsRepository, 
            TimeSpan apiTimeout, 
            RabbitMqSubscriber<TradingSignal> messageProducer, 
            bool enabled)
        {
            this.exchanges = exchanges.ToDictionary(k => k.Name);
            this.logger = logger;
            _acknowledHandler = acknowledHandler;
            _tradeHandler = tradeHandler;
            this.translatedSignalsRepository = translatedSignalsRepository;
            this.apiTimeout = apiTimeout;
            _messageProducer = messageProducer;
            _enabled = enabled;
            if (enabled)
            {
                messageProducer.Subscribe(Handle);
            }
        }

        public Task Handle(TradingSignal message)
        {
            if (message == null || message.Instrument == null || string.IsNullOrEmpty(message.Instrument.Name))
            {
                return logger.WriteWarningAsync(
                    nameof(TradingSignalsHandler),
                    nameof(Handle),
                    message?.ToString(),
                    "Received an unconsistent signal");
            }

            if (!exchanges.ContainsKey(message.Instrument.Exchange))
            {
                return logger.WriteWarningAsync(
                        nameof(TradingSignalsHandler),
                        nameof(Handle),
                        message.ToString(),
                        $"Received a trading signal for unconnected exchange {message.Instrument.Exchange}");
            }

            return HandleTradingSignals(exchanges[message.Instrument.Exchange], message);
        }

        private async Task HandleTradingSignals(Exchange exchange, TradingSignal signal)
        {
            await logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleTradingSignals), signal.ToString(), "New trading signal to be handled.");

            var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RabbitQueue, signal);

            try
            {
                switch (signal.Command)
                {
                    case OrderCommand.Create:
                        await HandleCreation(signal, translatedSignal, exchange);
                        break;
                    case OrderCommand.Cancel:
                        await HandleCancellation(signal, translatedSignal, exchange);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(signal));
                }
            }
            catch (Exception e)
            {
                translatedSignal.Failure(e);
            }
            finally
            {
                translatedSignalsRepository.Save(translatedSignal);

                await logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleTradingSignals),
                    signal.ToString(), "Signal handled. Waiting for another one.");
            }
        }

        private async Task HandleCreation(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            Exchange exchange)
        {
            try
            {
                if (!signal.IsTimeInThreshold(tradingSignalsThreshold))
                {
                    translatedSignal.Failure("The signal is too old");

                    await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCreation),
                        signal.ToString(),
                        "Skipping old signal");

                    return;
                }

                var executedTrade = await exchange.AddOrderAndWaitExecution(signal, translatedSignal, apiTimeout);

                bool orderAdded = executedTrade.ExecutionStatus == OrderExecutionStatus.New ||
                                  executedTrade.ExecutionStatus == OrderExecutionStatus.Pending;

                bool orderFilled = executedTrade.ExecutionStatus == OrderExecutionStatus.Fill ||
                                   executedTrade.ExecutionStatus == OrderExecutionStatus.PartialFill;
                if (orderAdded || orderFilled)
                {
                    await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCreation),
                        signal.ToString(),
                        "Created new order");
                }
                else
                {
                    await logger.WriteWarningAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCreation),
                        signal.ToString(),
                        $"Added order is in unexpected status: {executedTrade}");

                    translatedSignal.Failure($"Added order is in unexpected status: {executedTrade}");
                }
    
                
                await _acknowledHandler.Handle(CreateAcknowledgement(exchange, orderAdded, signal, translatedSignal));
                

                if (orderFilled)
                {
                    await _tradeHandler.Handle(executedTrade);
                }
            }
            catch (ApiException e)
            {
                await logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCreation), signal.ToString(), e.Message);
                translatedSignal.Failure(e);
                await _acknowledHandler.Handle(CreateAcknowledgement(exchange, false, signal, translatedSignal, e));
            }
            catch (Exception e)
            {
                await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                    nameof(HandleCreation),
                    signal.ToString(),
                    e);
                
                translatedSignal.Failure(e);
                
                await _acknowledHandler.Handle(CreateAcknowledgement(exchange, false, signal, translatedSignal, e));
            }
        }

        private async Task HandleCancellation(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            Exchange exchange)
        {
            try
            {
                var executedTrade = await exchange.CancelOrderAndWaitExecution(signal, translatedSignal, apiTimeout);

                if (executedTrade.ExecutionStatus == OrderExecutionStatus.Cancelled)
                {
                    

                    
                    await _tradeHandler.Handle(executedTrade);

                    
                }
                else
                {
                    var message =
                        $"Executed trade status {executedTrade.ExecutionStatus} after calling 'exchange.CancelOrderAndWaitExecution'";
                    translatedSignal.Failure(message);
                    await logger.WriteWarningAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCancellation),
                        signal.ToString(),
                        message);
                }
            }
            catch (ApiException e)
            {
                translatedSignal.Failure(e);
                await logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCancellation),
                    signal.ToString(),
                    e.Message);
            }
            catch (Exception e)
            {
                translatedSignal.Failure(e);
                await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                    nameof(HandleCancellation),
                    signal.ToString(),
                    e);
            }
        }

        private static ExecutionReport CreateAcknowledgement(IExchange exchange, bool success,
            TradingSignal arrivedSignal, TranslatedSignalTableEntity translatedSignal, Exception exception = null)
        {
            var ack = new ExecutionReport
            {
                Success = success,
                Instrument = arrivedSignal.Instrument,
                ClientOrderId = arrivedSignal.OrderId,
                ExchangeOrderId = translatedSignal.ExternalId,
                Message = translatedSignal.ErrorMessage
            };

            if (exception != null)
            {
                switch (exception)
                {
                    case InsufficientFundsException _:
                        ack.FailureType = OrderStatusUpdateFailureType.InsufficientFunds;
                        break;
                    case ApiException _:
                        ack.FailureType = OrderStatusUpdateFailureType.ExchangeError;
                        break;
                    default:
                        ack.FailureType = OrderStatusUpdateFailureType.ConnectorError;
                        break;
                }
            }
            else
            {
                ack.FailureType = OrderStatusUpdateFailureType.None;
            }

            return ack;
        }

        public void Start()
        {
            if (_enabled)
            {
                _messageProducer.Start();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose here
        }

        public void Stop()
        {
            if (_enabled)
            {
                _messageProducer.Stop();
            }
        }
    }
}
