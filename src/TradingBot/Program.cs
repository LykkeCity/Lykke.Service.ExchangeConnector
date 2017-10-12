using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TradingBot
{
    internal sealed class Program
    {
        static void Main(string[] args)
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

                host.Run(); // returns on Ctrl+C

                Console.WriteLine("The service is stopped.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
