using System;
using System.IO;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using TradingBot.Infrastructure.Configuration;

namespace Lykke.Service.ExchangeConnector.Tests.Bitfinex
{
    internal static class BitfinexHelpers
    {
        private static readonly string _configFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "testsettings.Bitfinex.json");

        public static BitfinexExchangeConfiguration GetBitfinexConfiguration()
        {
            return GetBitfinexSettingsMenager().CurrentValue;
        }

        public static IReloadingManager<BitfinexExchangeConfiguration> GetBitfinexSettingsMenager()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(_configFilePath, optional: false, reloadOnChange: true);
            var configuration = configBuilder.Build();

            configuration["SettingsUrl"] = _configFilePath;
            var settingsManager = configuration.LoadSettings<BitfinexExchangeConfiguration>("SettingsUrl");

            return settingsManager;
        }
    }
}
