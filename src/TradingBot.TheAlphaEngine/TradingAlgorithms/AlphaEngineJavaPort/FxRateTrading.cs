using System;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort
{
	public class FxRateTrading
	{
        private readonly ILogger logger = Logging.CreateLogger<FxRateTrading>();

		CoastlineTrader[] coastTraderLong, coastTraderShort;
		String FXrate;
		Liquidity liquidity;
		//double currentTime, oneDay;
		bool init;
        DateTime currentTime;

		public FxRateTrading(String rate, int nbOfCoastTraders, double[] deltas)
		{
            //currentTime = DateTime.UtcNow;
			//oneDay = 24.0 * 60.0 * 60.0 * 1000.0;
			FXrate = rate;

			coastTraderLong = new CoastlineTrader[nbOfCoastTraders];
			coastTraderShort = new CoastlineTrader[nbOfCoastTraders];

            for (int i = 0; i < coastTraderLong.Length; ++i)
			{
                coastTraderLong[i] = new CoastlineTrader(deltas[i], deltas[i], deltas[i], deltas[i], rate, OrderType.Long);
                coastTraderShort[i] = new CoastlineTrader(deltas[i], deltas[i], deltas[i], deltas[i], rate, OrderType.Short);
			}
			init = false;
		}

		public void RunTradingAsymm(TickPrice price)
		{
			if (!init)
			{
				init = true;
                currentTime = price.Time;
				//liquidity = new Liquidity(price, 0.05/100.0, 0.1/100.0, 50);
			}

			//liquidity.Trigger(price);
			//System.out.println("liqEMA: " + liquidity.liqEMA);

            for (int i = 0; i < coastTraderLong.Length; ++i)
			{
                coastTraderLong[i].RunPriceAsymm(price, coastTraderShort[i].TotalPosition);
                coastTraderShort[i].RunPriceAsymm(price, coastTraderLong[i].TotalPosition);
			}

            // TODO: move it outside
   //         if (price.Time >= currentTime.AddDays(1))
			//{
			//	while (currentTime <= price.Time)
   //                 currentTime = currentTime.AddDays(1);

			//	//PrintDataAsymm(price);
			//}
		}

        public string PrintDataAsymm(TickPrice price)
		{

            decimal totalPos = 0m;
            decimal totalShort = 0m; 
            decimal totalLong = 0m; 

            double totalPnl = 0.0; 
            double totalPnlPerc = 0.0;
			
            for (int i = 0; i < coastTraderLong.Length; ++i)
			{
                totalLong += coastTraderLong[i].TotalPosition;
                totalShort += coastTraderShort[i].TotalPosition;
                totalPos += (coastTraderLong[i].TotalPosition + coastTraderShort[i].TotalPosition);
				//totalPnl += (coastTraderLong[i].pnl + coastTraderLong[i].tempPnl + coastTraderLong[i].computePnlLastPrice()
				//		+ coastTraderShort[i].pnl + coastTraderShort[i].tempPnl + coastTraderShort[i].computePnlLastPrice());
				//totalPnlPerc += (coastTraderLong[i].pnlPerc + (coastTraderLong[i].tempPnl + coastTraderLong[i].computePnlLastPrice()) / coastTraderLong[i].cashLimit * coastTraderLong[i].profitTarget
						//+ coastTraderShort[i].pnlPerc + (coastTraderShort[i].tempPnl + coastTraderShort[i].computePnlLastPrice()) / coastTraderShort[i].cashLimit * coastTraderShort[i].profitTarget);
			}
			//double tempSurpScale = Math.sqrt(50)*(liquidity.surp - liquidity.H1)/Math.sqrt(liquidity.H2); 
			
            return $"{currentTime}, {totalPnl}, {totalPnlPerc}, {totalPos}, {totalLong}, {totalShort}, {price}";
			
		}
	}

}
