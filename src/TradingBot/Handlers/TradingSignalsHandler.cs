using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class TradingSignalsHandler : Handler<InstrumentTradingSignals>
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
        
        
        public override Task Handle(InstrumentTradingSignals message)
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
        
        public async Task HandleTradingSignals(Exchange exchange, InstrumentTradingSignals signals)
        {
            if (signals.Instrument == null || string.IsNullOrEmpty(signals.Instrument.Name) ||
                signals.TradingSignals == null || !signals.TradingSignals.Any())
            {
                return;
            }

            var instrumentName = signals.Instrument.Name;

            foreach (var arrivedSignal in signals.TradingSignals)
            {
                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RabbitQueue, signals.Instrument.Exchange, instrumentName, arrivedSignal);

                try
                {
                    TradingSignal existing;

                    switch (arrivedSignal.Command)
                    {
                        case OrderCommand.Create:
                            try
                            {
                                if (!arrivedSignal.IsTimeInThreshold(tradingSignalsThreshold))
                                {
                                    await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                        nameof(HandleTradingSignals),
                                        nameof(HandleTradingSignals),
                                        $"Skipping old signal {arrivedSignal}");

                                    translatedSignal.Failure("The signal is too old");
                                    break;
                                }

                                var result = await retryTwoTimesPolicy.ExecuteAndCaptureAsync(() =>
                                    exchange.AddOrder(signals.Instrument, arrivedSignal, translatedSignal));

                                if (result.Outcome == OutcomeType.Successful)
                                {
                                    await logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                        nameof(HandleTradingSignals),
                                        string.Empty,
                                        $"Created new order {arrivedSignal}");
                                }
                                else
                                {
                                    await logger.WriteErrorAsync(nameof(TradingSignalsHandler),
                                        nameof(HandleTradingSignals),
                                        string.Empty,
                                        result.FinalException);

                                    translatedSignal.Failure(result.FinalException);
                                }
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
                                    exchange.CancelOrder(signals.Instrument, arrivedSignal, translatedSignal));

                                if (result.Outcome == OutcomeType.Successful)
                                {
                                    logger.WriteInfoAsync(nameof(TradingSignalsHandler),
                                        nameof(HandleTradingSignals),
                                        string.Empty,
                                        $"Canceled order {arrivedSignal}").Wait();
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
        }
    }
}
