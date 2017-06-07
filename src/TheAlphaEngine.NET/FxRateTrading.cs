using System;
using TradingBot.Common.Trading;

namespace TheAlphaEngine.NET
{
	public class FxRateTrading
	{
		CoastlineTrader[] coastTraderLong, coastTraderShort;
		String FXrate;
		Liquidity liquidity;
		//double currentTime, oneDay;
		bool init;
        DateTime currentTime;

		public FxRateTrading(String rate, int nbOfCoastTraders, double[] deltas)
		{
            currentTime = DateTime.UtcNow;
			//oneDay = 24.0 * 60.0 * 60.0 * 1000.0;
			FXrate = rate;

			coastTraderLong = new CoastlineTrader[nbOfCoastTraders];
			coastTraderShort = new CoastlineTrader[nbOfCoastTraders];

            for (int i = 0; i < coastTraderLong.Length; ++i)
			{
				coastTraderLong[i] = new CoastlineTrader(deltas[i], deltas[i], deltas[i], deltas[i], rate, 1);
				coastTraderShort[i] = new CoastlineTrader(deltas[i], deltas[i], deltas[i], deltas[i], rate, -1);
			}
			init = false;
		}

		public bool RunTradingAsymm(TickPrice price)
		{
			if (!init)
			{
				init = true;
				//liquidity = new Liquidity(price, 0.05/100.0, 0.1/100.0, 50);
			}

			//liquidity.Trigger(price);
			//System.out.println("liqEMA: " + liquidity.liqEMA);

            for (int i = 0; i < coastTraderLong.Length; ++i)
			{
                coastTraderLong[i].RunPriceAsymm(price, coastTraderShort[i].TotalPosition);
                coastTraderShort[i].RunPriceAsymm(price, coastTraderLong[i].TotalPosition);
			}

            if (price.Time >= currentTime.AddDays(1))
			{
				while (currentTime <= price.Time)
                    currentTime = currentTime.AddDays(1);

				//PrintDataAsymm(currentTime);
			}
			return true;
		}

		//bool PrintDataAsymm(double time)
		//{
		//	String sep = new String(System.getProperty("file.separator"));
		//	String folder = new String(sep + "home" + sep + "agolub" + sep + "workspace" + sep + "InvestmentStrategy" + sep + FXrate.toString() + "DataAsymmLiq.dat");
		//	FileWriter fw = null;

		//	try
		//	{
		//		double totalPos = 0.0, totalShort = 0.0, totalLong = 0.0; double totalPnl = 0.0; double totalPnlPerc = 0.0;
		//		fw = new FileWriter(folder, true);
		//		double price = -1.0;
		//		for (int i = 0; i < coastTraderLong.length; ++i)
		//		{
		//			if (i == 0)
		//			{
		//				price = coastTraderLong[i].lastPrice;
		//			}
		//			totalLong += coastTraderLong[i].tP;
		//			totalShort += coastTraderShort[i].tP;
		//			totalPos += (coastTraderLong[i].tP + coastTraderShort[i].tP);
		//			totalPnl += (coastTraderLong[i].pnl + coastTraderLong[i].tempPnl + coastTraderLong[i].computePnlLastPrice()
		//					+ coastTraderShort[i].pnl + coastTraderShort[i].tempPnl + coastTraderShort[i].computePnlLastPrice());
		//			totalPnlPerc += (coastTraderLong[i].pnlPerc + (coastTraderLong[i].tempPnl + coastTraderLong[i].computePnlLastPrice()) / coastTraderLong[i].cashLimit * coastTraderLong[i].profitTarget
		//					+ coastTraderShort[i].pnlPerc + (coastTraderShort[i].tempPnl + coastTraderShort[i].computePnlLastPrice()) / coastTraderShort[i].cashLimit * coastTraderShort[i].profitTarget);
		//		}
		//		//double tempSurpScale = Math.sqrt(50)*(liquidity.surp - liquidity.H1)/Math.sqrt(liquidity.H2); 
		//		fw.append((long)time + "," + totalPnl + "," + totalPnlPerc + "," + totalPos + "," + totalLong + "," + totalShort + "," + price + "\n");
		//		fw.close();
		//	}
		//	catch (Exception e)
		//	{
		//		System.out.println("Failed opening DC thresh file! " + e.getMessage());
		//		return false;
		//	}
		//	return true;
		//}
	}

}
