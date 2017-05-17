using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TradingBot.Infrastructure;
using TradingBot.Trading;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// This agent subscribes for new IntrinsicTimeEvents and generate Trading signals
    /// </summary>
    public class TradingAgent
    {
        private ILogger logger = Logging.CreateLogger<TradingAgent>();
        
        public string Instrument { get; }

        public event Action<Signal> NewSignalGenerated;

        public TradingAgent(string instrument)
        {
            Instrument = instrument;
        }

        private List<Signal> signals = new List<Signal>();

        public void HandleEvent(IntrinsicTimeEvent intrinsicTimeEvent)
        {
            logger.LogInformation($"New event received: {intrinsicTimeEvent}");

            if (intrinsicTimeEvent is Overshoot)
            {
                if (intrinsicTimeEvent.Mode == AlgorithmMode.Down)
                {
                    AddNewSignal(new Signal(SignalType.Long, intrinsicTimeEvent.Price, 1));
                    // it's required to get units from engine agent
                }
                else
                {
                    AddNewSignal(new Signal(SignalType.Short, intrinsicTimeEvent.Price, 1));
                }
            }
        }

        private void AddNewSignal(Signal signal)
        {
            signals.Add(signal);

            NewSignalGenerated?.Invoke(signal);
        }
    }
}
