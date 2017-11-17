using System;
using System.IO;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using TradingBot.Infrastructure.Configuration;

namespace Lykke.Service.ExchangeConnector.Tests.GDAX
{
    internal class GdaxHelpers
    {
        private static string _configFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "testsettings.Gdax.json");

        public static GdaxExchangeConfiguration GetGdaxConfiguration()
        {
            return GetGdaxSettingsMenager().CurrentValue;
        }

        public static IReloadingManager<GdaxExchangeConfiguration> GetGdaxSettingsMenager()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(_configFilePath, optional: false, reloadOnChange: true);
            var configuration = configBuilder.Build();

            configuration["SettingsUrl"] = _configFilePath;
            var settingsManager = configuration.LoadSettings<GdaxExchangeConfiguration>("SettingsUrl");

            return settingsManager;
        }
    }
}
