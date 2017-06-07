using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;

namespace TradingBot.Common.Communications
{
	public class Reconnector
	{
		private ILogger Logger = Logging.CreateLogger<Reconnector>();

		private readonly int times;
		private readonly TimeSpan pause;

		public Reconnector(int times, TimeSpan pause)
		{
			this.pause = pause;
			this.times = times;
		}

		private bool stopTrying;
		private int counter;

		public async Task<bool> ConnectAsync(Func<Task<bool>> connect, CancellationToken cancellationToken)
		{
			try
			{
				stopTrying = false;
				counter = times;

				while (!stopTrying && !cancellationToken.IsCancellationRequested)
				{
					bool connected = await connect();

					if (connected)
					{
						return true;
					}
					else
					{
						Logger.LogInformation($"Unsuccessful connection. Will try connect in {pause}.");

						if (times != 0 && --counter <= 0)
						{
							Logger.LogInformation($"There was the last attempt. Connection unsuccessful");
							return false;
						}
					}

					await Task.Delay(pause, cancellationToken);
				}

				return false;
			}
			catch (Exception ex)
			{
				Logger.LogError(new EventId(), ex, "Exception during connection");
				return false;
			}
		}

		public async Task<bool> Connect(Func<bool> connect, CancellationToken cancellationToken)
		{
			try
			{
				stopTrying = false;
				counter = times;

				while (!stopTrying && !cancellationToken.IsCancellationRequested)
				{
					bool connected = connect();

					if (connected)
					{
						return true;
					}
					else
					{
						Logger.LogInformation($"Unsuccessful connection. Will try connect in {pause}.");

						if (times != 0 && --counter <= 0)
						{
							Logger.LogInformation($"There was the last attempt. Connection unsuccessful");
							return false;
						}
					}

					await Task.Delay(pause, cancellationToken);
				}

				return false;
			}
			catch (Exception ex)
			{
				Logger.LogError(new EventId(), ex, "Exception during connection");
				return false;
			}
		}
	}
}
