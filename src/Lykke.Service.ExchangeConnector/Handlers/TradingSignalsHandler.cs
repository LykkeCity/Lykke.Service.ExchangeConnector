using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal class TradingSignalsHandler : Handler<TradingSignal>
    {
        private readonly IReadOnlyDictionary<string, Exchange> exchanges;
        private readonly ILog logger;
        private readonly TranslatedSignalsRepository translatedSignalsRepository;
        private readonly TimeSpan tradingSignalsThreshold = TimeSpan.FromMinutes(10);
        private readonly TimeSpan apiTimeout;

        public TradingSignalsHandler(Dictionary<string, Exchange> exchanges, ILog logger, TranslatedSignalsRepository translatedSignalsRepository, TimeSpan apiTimeout)
        {
            this.exchanges = exchanges;
            this.logger = logger;
            this.translatedSignalsRepository = translatedSignalsRepository;
            this.apiTimeout = apiTimeout;
        }
        
        public override Task Handle(TradingSignal message)
        {
            if (message == null || message.Instrument == null || string.IsNullOrEmpty(message.Instrument.Name))
            {
                return logger.WriteWarningAsync(
                    nameof(TradingSignalsHandler),
                    nameof(Handle),
                    message?.ToString(),
                    $"Received an unconsistent signal");
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
                        throw new ArgumentOutOfRangeException();
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

                bool orderAdded = executedTrade.Status == OrderExecutionStatus.New ||
                                  executedTrade.Status == OrderExecutionStatus.Pending;

                bool orderFilled = executedTrade.Status == OrderExecutionStatus.Fill ||
                                   executedTrade.Status == OrderExecutionStatus.PartialFill;
    
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
    
                logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCreation), signal.ToString(),
                    "About to call AcknowledgementsHandlers").Wait();
                await exchange.CallAcknowledgementsHandlers(CreateAcknowledgement(exchange, orderAdded, signal, translatedSignal));
                logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCreation), signal.ToString(),
                    "AcknowledgementsHandlers are called").Wait();

                if (orderFilled)
                {
                    logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCreation), signal.ToString(),
                        "About to call ExecutedTradeHandlers").Wait();
                    await exchange.CallExecutedTradeHandlers(executedTrade);
                    logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCreation), signal.ToString(),
                        "ExecutedTradeHandlers are called").Wait();
                }
            }
            catch (Exception e)
            {
                await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                    nameof(HandleCreation),
                    signal.ToString(),
                    e);
                
                translatedSignal.Failure(e);
                
                await exchange.CallAcknowledgementsHandlers(CreateAcknowledgement(exchange, false, signal, translatedSignal, e));
            }
        }

        private async Task HandleCancellation(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            Exchange exchange)
        {
            try
            {
                var executedTrade = await exchange.CancelOrderAndWaitExecution(signal, translatedSignal, apiTimeout);
                            
                if (executedTrade.Status == OrderExecutionStatus.Cancelled)
                {
                    logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCancellation),
                        signal.ToString(),
                        "Canceled order. About to call ExecutedTradeHandlers").Wait();

                    
                    await exchange.CallExecutedTradeHandlers(executedTrade);

                    logger.WriteInfoAsync(nameof(TradingSignalsHandler), nameof(HandleCancellation), signal.ToString(),
                        "ExecutedTradeHandlers are called").Wait();
                }
                else
                {
                    var message = $"Executed trade status {executedTrade.Status} after calling 'exchange.CancelOrderAndWaitExecution'";
                    translatedSignal.Failure(message);
                    await logger.WriteWarningAsync(nameof(TradingSignalsHandler),
                        nameof(HandleCancellation),
                        signal.ToString(),
                        message);
                }
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

        private static OrderStatusUpdate CreateAcknowledgement(IExchange exchange, bool success,
            TradingSignal arrivedSignal, TranslatedSignalTableEntity translatedSignal, Exception exception = null)
        {
            var ack = new OrderStatusUpdate
            {
                Success = success,
                Exchange = exchange.Name,
                InstrumentName = arrivedSignal.Instrument.Name,
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
    }
}
