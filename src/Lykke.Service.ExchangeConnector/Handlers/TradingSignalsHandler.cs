using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal class TradingSignalsHandler : Handler<TradingSignal>
    {
        private readonly Dictionary<string, Exchange> exchanges;
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
            if (!exchanges.ContainsKey(message.Instrument.Exchange))
            {
                logger.WriteWarningAsync(
                        nameof(TradingSignalsHandler),
                        nameof(Handle),
                        string.Empty,
                        $"Received a trading signal for unconnected exchange {message.Instrument.Exchange}")
                    .Wait();
                return Task.FromResult(0);
            }
            else
            {
                return HandleTradingSignals(exchanges[message.Instrument.Exchange], message);    
            }
        }

        public async Task HandleTradingSignals(Exchange exchange, TradingSignal signal)
        {
            if (signal == null || signal.Instrument == null || string.IsNullOrEmpty(signal.Instrument.Name))
            {
                return;
            }

            var instrumentName = signal.Instrument.Name;

            var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RabbitQueue, signal);

            try
            {
                switch (signal.Command)
                {
                    case OrderCommand.Create:
                        try
                        {
                            if (!signal.IsTimeInThreshold(tradingSignalsThreshold))
                            {
                                await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    signal.ToString(),
                                    "Skipping old signal");

                                translatedSignal.Failure("The signal is too old");
                                break;
                            }

                            bool orderAdded = await exchange.AddOrder(signal, translatedSignal);

                            if (orderAdded)
                            {
                                await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    signal.ToString(),
                                    "Created new order");
                            }
                            else
                            {
                                await logger.WriteWarningAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    signal.ToString(),
                                    "exchange.AddOrder have returned false");

                                translatedSignal.Failure("exchange.AddOrder have returned false");
                            }

                            await exchange.CallAcknowledgementsHandlers(CreateAcknowledgement(exchange, orderAdded, instrumentName, signal, translatedSignal));
                        }
                        catch (Exception e)
                        {
                            await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                nameof(HandleTradingSignals),
                                signal.ToString(),
                                e);
                            
                            translatedSignal.Failure(e);
                            
                            await exchange.CallAcknowledgementsHandlers(CreateAcknowledgement(exchange, false, instrumentName, signal, translatedSignal, e));
                        }
                        break;

                    case OrderCommand.Edit:
                        throw new NotSupportedException("Do not support edit signal");

                    case OrderCommand.Cancel:

                        try
                        {
                            var executedTrade = await exchange.CancelOrderAndWaitExecution(signal, translatedSignal, apiTimeout);
                            
                            if (executedTrade.Status == ExecutionStatus.Cancelled)
                            {
                                logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    signal.ToString(),
                                    "Canceled order").Wait();

                                await exchange.CallExecutedTradeHandlers(executedTrade);
                            }
                            else
                            {
                                var message = $"Executed trade status {executedTrade.Status} after calling 'exchange.CancelOrderAndWaitExecution'";
                                translatedSignal.Failure(message);
                                await logger.WriteWarningAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    signal.ToString(),
                                    message);
                            }
                        }
                        catch (Exception e)
                        {
                            translatedSignal.Failure(e);
                            await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                nameof(HandleTradingSignals),
                                signal.ToString(),
                                e);
                        }

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
            }
            
        }

        private static Acknowledgement CreateAcknowledgement(Exchange exchange, bool success, string instrumentName,
            TradingSignal arrivedSignal, TranslatedSignalTableEntity translatedSignal, Exception exception = null)
        {
            var ack = new Acknowledgement()
            {
                Success = success,
                Exchange = exchange.Name,
                Instrument = instrumentName,
                ClientOrderId = arrivedSignal.OrderId,
                ExchangeOrderId = translatedSignal.ExternalId,
                Message = translatedSignal.ErrorMessage
            };

            if (exception != null)
            {
                switch (exception)
                {
                    case InsufficientFundsException _:
                        ack.FailureType = AcknowledgementFailureType.InsufficientFunds;
                        break;
                    case ApiException _:
                        ack.FailureType = AcknowledgementFailureType.ExchangeError;
                        break;
                    default:
                        ack.FailureType = AcknowledgementFailureType.ConnectorError;
                        break;
                }
            }
            else
            {
                ack.FailureType = AcknowledgementFailureType.None;
            }
            
            return ack;
        }
    }
}
