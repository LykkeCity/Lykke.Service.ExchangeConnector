using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class ExchangeConverters
    {
        private readonly IReadOnlyCollection<CurrencySymbol> _currencySymbols;
        private readonly string _exchangeName;
        private readonly bool _useSupportedCurrencySymbolsAsFilter;

        public ExchangeConverters(IReadOnlyCollection<CurrencySymbol> currencySymbols, 
            string exchangeName, bool useSupportedCurrencySymbolsAsFilter)
        {
            _currencySymbols = currencySymbols;
            _useSupportedCurrencySymbolsAsFilter = useSupportedCurrencySymbolsAsFilter;
            _exchangeName = exchangeName;
        }

        public string LykkeSymbolToExchangeSymbol(string lykkeSymbol)
        {
            var foundSymbol = _currencySymbols.FirstOrDefault(s => s.LykkeSymbol == lykkeSymbol);
            if (foundSymbol == null && _useSupportedCurrencySymbolsAsFilter)
            {
                throw new ArgumentException($"Symbol {lykkeSymbol} is not mapped to {_exchangeName} value");
            }
            return foundSymbol?.ExchangeSymbol ?? lykkeSymbol;
        }

        public Instrument ExchangeSymbolToLykkeInstrument(string exchangeSymbol)
        {
            var foundSymbol = _currencySymbols.FirstOrDefault(s => s.ExchangeSymbol == exchangeSymbol);
            if (foundSymbol == null && _useSupportedCurrencySymbolsAsFilter)
            {
                throw new ArgumentException(
                    $"Symbol {exchangeSymbol} in {_exchangeName} is not mapped to lykke value");
            }
            return new Instrument(_exchangeName, foundSymbol?.LykkeSymbol ?? exchangeSymbol);
        }
    }
}
