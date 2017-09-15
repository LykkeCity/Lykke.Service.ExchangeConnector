using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TradingBot
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
	            var host = new WebHostBuilder()
		            .UseKestrel()
		            .UseContentRoot(Directory.GetCurrentDirectory())
		            .UseStartup<Startup>()
		            .Build();

	            host.Run(); // returns on Ctrl+C

	            Console.WriteLine("The service is stopped.");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
