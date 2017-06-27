﻿﻿using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public static class Logging // TODO: move to Common
    {
        public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory()
            .AddConsole(LogLevel.Debug);

        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    }
}
