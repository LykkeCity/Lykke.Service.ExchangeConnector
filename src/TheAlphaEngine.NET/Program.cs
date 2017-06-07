using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Configuration;
using TradingBot.Common.Communications;
using Newtonsoft.Json;
using TradingBot.Common.Trading;

namespace TheAlphaEngine.NET
{
    class Program
    {
		private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
			var configBuilder = new ConfigurationBuilder();
			configBuilder
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true)
				.AddCommandLine(args);

            var config = configBuilder.Build();

            var rabbitConfig = config.GetSection("rabbitMQ").Get<RabbitMQConfiguration>();

            var ctSource = new CancellationTokenSource();
            var token = ctSource.Token;


            var engine = new AlphaEngine("");

            var task = Task.Run(async () => {
				using (var rabbit = new RabbitMQClient(rabbitConfig))
				{
                    bool connected = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(15))
                        .Connect(rabbit.OpenConnection, token);
                    
					if (!connected)
						return;

                    Logger.LogDebug("RabbitMQ connected");

                    rabbit.AddConsumer(bytes => {
                        Logger.LogDebug("Bytes received");

                        string serialized = Encoding.UTF8.GetString(bytes);
                        Logger.LogDebug($"Serialized string: {serialized}");

                        var prices = JsonConvert.DeserializeObject<TickPrice[]>(serialized);

                        Logger.LogDebug($"Received {prices.Length} prices");

                        engine.OnPriceChanged(prices);

                        Logger.LogDebug($"engine result: ");
                    });
                    
                    while (!token.IsCancellationRequested)
					{
						await Task.Delay(TimeSpan.FromSeconds(5));
						Logger.LogDebug($"AlphaEngine Heartbeat: {DateTime.Now}");
					}
				}
            });

			Logger.LogInformation("Press Ctrl+C for exit");

			Console.CancelKeyPress += (sender, eventArgs) =>
				{
                    eventArgs.Cancel = true; // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    ctSource.Cancel();

					if (task.Status == TaskStatus.Running)
					{
						Console.WriteLine("Waiting for prices cycle completion");
						task.Wait();
					}
				};


			task.Wait();

			Console.WriteLine("Applicatoin stopped.");
			Environment.Exit(0);
        }
    }
}
