using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace TradingBot
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseApplicationInsights()
                    .Build();

                await host.RunAsync(); // returns on Ctrl+C

                Console.WriteLine("The service is stopped.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
