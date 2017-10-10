using System;
using System.Linq;
using Lykke.Common.ApiLibrary.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using AzureStorage;
using AzureStorage.Tables;
using TradingBot.Communications;
using TradingBot.Repositories;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IContainer ApplicationContainer { get; private set; }

        private ExchangeConnectorApplication _app;

        private INoSQLTableStorage<PriceTableEntity> _pricesStorage;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseMiddleware<StatusCodeExceptionHandler>();

            app.UseMvcWithDefaultRoute();

            app.UseSwagger();
            app.UseSwaggerUi();

            // Dispose resources that have been resolved in the application container
            appLifetime.ApplicationStarted.Register(StartHandler);
            appLifetime.ApplicationStopping.Register(ShutDownHandler);
            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            app.Run(async (context) =>
            {
                // TODO: get for all enabled exchanges
                var report = await StatusReport.Create(_pricesStorage);

                var response = $"Status: {(report.LastPrices.Any() ? "OK" : "FAIL")}\n\nLast prices:\n{string.Join("\n", report.LastPrices)}";

                await context.Response.WriteAsync(response);
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILog log = new LogToConsole();
            string appName = nameof(TradingBot);

            try
            {
                // Add framework services.
                services.AddMvc();

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "ExchangeConnectorAPI");
                    options.OperationFilter<AddSwaggerAuthorizationHeaderParameter>();
                });

                var settingsManager = Configuration.LoadSettings<TradingBotSettings>("SettingsUrl");
                var topSettings = settingsManager.CurrentValue;
                var settings = topSettings.TradingBot;


                // Register dependencies, populate the services from
                // the collection, and build the container. If you want
                // to dispose of the container at the end of the app,
                // be sure to keep a reference to it as a property or field.
                var builder = new ContainerBuilder();

                if (settings.AzureStorage.Enabled)
                {
                    var slackService = services.UseSlackNotificationsSenderViaAzureQueue(topSettings.SlackNotifications.AzureQueue, log);
                    var tableStorage = AzureTableStorage<LogEntity>.Create(
                        settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "logsExchangeConnector", log);
                    builder.RegisterInstance(tableStorage).As<INoSQLTableStorage<LogEntity>>().SingleInstance();
                    var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(appName, tableStorage, log);
                    var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, log);
                    var logToTable = new LykkeLogToAzureStorage(
                        appName,
                        persistenceManager,
                        slackNotificationsManager,
                        log);
                    logToTable.Start();

                    log = new LogAggregate()
                        .AddLogger(log)
                        .AddLogger(logToTable)
                        .CreateLogger();
                }

                ApiKeyAuthAttribute.ApiKey = settings.AspNet.ApiKey;
                //   SignedModelAuthAttribute.ApiKey = settings.AspNet.ApiKey; //TODO use it somewhere

                builder.RegisterInstance(log).As<ILog>().SingleInstance();

                builder.RegisterInstance(settings).As<AppSettings>().SingleInstance();

                _pricesStorage = AzureTableStorage<PriceTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "kraken", log);

                var fixMessagesStorage = AzureTableStorage<FixMessageTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "fixMessages", log);
                builder.RegisterInstance(fixMessagesStorage).As<INoSQLTableStorage<FixMessageTableEntity>>().SingleInstance();

                var javaLogsStorage = AzureTableStorage<JavaLogEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "logsAlphaEngine", log);
                builder.RegisterInstance(javaLogsStorage).As<INoSQLTableStorage<JavaLogEntity>>().SingleInstance();

                var javaEventsStorage = AzureTableStorage<JavaIntrinsicEventEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "intrinsicEvents", log);
                builder.RegisterInstance(javaEventsStorage).As<INoSQLTableStorage<JavaIntrinsicEventEntity>>().SingleInstance();

                _app = new ExchangeConnectorApplication(settings, settingsManager, fixMessagesStorage, log);
                builder.RegisterInstance(_app).SingleInstance();

                builder.Populate(services);
                ApplicationContainer = builder.Build();

                // Create the IServiceProvider based on the container.
                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception exc)
            {
                log.WriteErrorAsync(
                    appName,
                    nameof(Startup),
                    nameof(ConfigureServices),
                    exc,
                    DateTime.UtcNow)
                   .Wait();
                throw;
            }
        }

        private void StartHandler()
        {
            _app.Start().Wait();
        }

        private void ShutDownHandler()
        {
            _app.Stop();
        }
    }
}
