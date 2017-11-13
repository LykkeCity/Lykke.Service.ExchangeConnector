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
using AzureStorage.Blob;
using AzureStorage.Tables;
using TradingBot.Communications;
using TradingBot.Repositories;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Modules;

namespace TradingBot
{
    internal sealed class Startup
    {
        private IConfigurationRoot Configuration { get; }
        private IContainer ApplicationContainer { get; set; }
        private ILog _log;


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
            // Register dependencies, populate the services from
            // the collection, and build the container. If you want
            // to dispose of the container at the end of the app,
            // be sure to keep a reference to it as a property or field.
            var builder = new ContainerBuilder();

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

                builder.RegisterInstance(settingsManager)
                    .As<IReloadingManager<TradingBotSettings>>();

                var topSettings = settingsManager.CurrentValue;
                var settings = topSettings.TradingBot;
                builder.RegisterInstance(settings)
                    .As<AppSettings>()
                    .SingleInstance();


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

                var signalsStorage = AzureTableStorage<TranslatedSignalTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "translatedSignals", new LogToConsole());
                builder.RegisterInstance(signalsStorage).As<INoSQLTableStorage<TranslatedSignalTableEntity>>().SingleInstance();

                var orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "orderBookSnapshots", new LogToConsole());
                builder.RegisterInstance(orderBookSnapshotStorage).As<INoSQLTableStorage<OrderBookSnapshotEntity>>().SingleInstance();

                var orderBookEventsStorage = AzureTableStorage<OrderBookEventEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "orderBookEvents", new LogToConsole());
                builder.RegisterInstance(orderBookEventsStorage).As<INoSQLTableStorage<OrderBookEventEntity>>().SingleInstance();

                var azureBlobStorage = AzureBlobStorage.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString));
                builder.RegisterInstance(azureBlobStorage).As<AzureBlobStorage>().SingleInstance();

                builder.RegisterModule(new ServiceModule(settings.Exchanges));

                builder.Populate(services);


                ApplicationContainer = builder.Build();

                _log = log;
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
            ApplicationContainer.Resolve<IApplicationFacade>().Start().GetAwaiter().GetResult();
            _log.WriteMonitorAsync("", "", "Started").GetAwaiter().GetResult();

        }

        private void ShutDownHandler()
        {
            _log?.WriteMonitorAsync("", "", "Terminating").GetAwaiter().GetResult();
            try
            {
                // NOTE: Service can't recieve and process requests here, so you can destroy all resources

                ApplicationContainer.Resolve<IApplicationFacade>().Stop();
            }
            catch (Exception ex)
            {
                _log?.WriteFatalErrorAsync(nameof(Startup), nameof(ShutDownHandler), "", ex);
                throw;
            }
        }
    }
}
