using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Polly;
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

        public TradingSignalsHandler(Dictionary<string, Exchange> exchanges, ILog logger, TranslatedSignalsRepository translatedSignalsRepository)
        {
            this.exchanges = exchanges;
            this.logger = logger;
            this.translatedSignalsRepository = translatedSignalsRepository;
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
        
        
        private readonly Policy retryTwoTimesPolicy = Policy
            .Handle<Exception>(x => !(x is InsufficientFundsException))
            .WaitAndRetryAsync(1, attempt => TimeSpan.FromSeconds(3));
        
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
                                    nameof(HandleTradingSignals),
                                    $"Skipping old signal {signal}");

                                translatedSignal.Failure("The signal is too old");
                                break;
                            }

                            var result = await retryTwoTimesPolicy.ExecuteAndCaptureAsync(() =>
                                exchange.AddOrder(signal, translatedSignal));

                            if (result.Outcome == OutcomeType.Successful)
                            {
                                await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    string.Empty,
                                    $"Created new order {signal}");
                            }
                            else
                            {
                                await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    string.Empty,
                                    result.FinalException);

                                translatedSignal.Failure(result.FinalException);
                            }

                            await exchange.CallAcknowledgementsHandlers(CreateAcknowledgement(exchange, result, instrumentName, signal, translatedSignal));
                        }
                        catch (Exception e)
                        {
                            await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                nameof(HandleTradingSignals),
                                string.Empty,
                                e);
                            
                            translatedSignal.Failure(e);
                        }
                        break;

                    case OrderCommand.Edit:
                        throw new NotSupportedException("Do not support edit signal");

                    case OrderCommand.Cancel:

                        try
                        {
                            var result = await retryTwoTimesPolicy.ExecuteAndCaptureAsync(() =>
                                exchange.CancelOrder(signal, translatedSignal));

                            if (result.Outcome == OutcomeType.Successful)
                            {
                                logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    string.Empty,
                                    $"Canceled order {signal}").Wait();

                                if (result.Result)
                                {
                                    await exchange.CallExecutedTradeHandlers(new ExecutedTrade(
                                        signal.Instrument,
                                        DateTime.UtcNow, signal.Price ?? 0, signal.Volume,
                                        signal.TradeType,
                                        signal.OrderId, ExecutionStatus.Cancelled));
                                }
                            }
                            else
                            {
                                translatedSignal.Failure(result.FinalException);
                                await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                    nameof(HandleTradingSignals),
                                    nameof(HandleTradingSignals),
                                    result.FinalException);
                            }
                        }
                        catch (Exception e)
                        {
                            translatedSignal.Failure(e);
                            await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                nameof(HandleTradingSignals),
                                string.Empty,
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

        private static Acknowledgement CreateAcknowledgement(Exchange exchange, PolicyResult<bool> result, string instrumentName,
            TradingSignal arrivedSignal, TranslatedSignalTableEntity translatedSignal)
        {
            var ack = new Acknowledgement()
            {
                Success = result.Outcome == OutcomeType.Successful,
                Exchange = exchange.Name,
                Instrument = instrumentName,
                ClientOrderId = arrivedSignal.OrderId,
                ExchangeOrderId = translatedSignal.ExternalId,
                Message = translatedSignal.ErrorMessage
            };

            if (result.FinalException != null)
            {
                switch (result.FinalException)
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
