using System;
using TradingBot.Common.Trading;

namespace TheAlphaEngine.NET
{
	public class Runner
	{
		decimal prevExtreme;
		DateTime prevExtremeTime;

		decimal prevDC;
		DateTime prevDCTime;

		decimal extreme;
		DateTime extremeTime;

        double deltaUp;
        double deltaDown;

        double deltaStarUp;
        double deltaStarDown;

		decimal osL;
		int type;
		bool initalized;
		decimal reference;

		string fileName;

        public int Type => type;

        public Runner(double threshUp, double threshDown, TickPrice price, string file, double dStarUp, double dStarDown)
		{
			prevExtreme = price.Mid; 
            prevExtremeTime = price.Time;

			prevDC = price.Mid; 
            prevDCTime = price.Time;

			extreme = price.Mid; 
            extremeTime = price.Time;

			reference = price.Mid;

			type = -1; 
            deltaUp = threshUp; 
            deltaDown = threshDown; 
            osL = 0.0m; 
            initalized = true;

            fileName = file;

			deltaStarUp = dStarUp; 
            deltaStarDown = dStarDown;
		}

        public Runner(double threshUp, double threshDown, decimal price, string file, double dStarUp, double dStarDown)
		{
			prevExtreme = price; 
            //prevExtremeTime = 0;

			prevDC = price; 
            //prevDCTime = 0;

			extreme = price; 
            //extremeTime = 0;

			reference = price;
			deltaStarUp = dStarUp; 
            deltaStarDown = dStarDown;

			type = -1; 
            deltaUp = threshUp; 
            deltaDown = threshDown; 
            osL = 0.0m; 
            initalized = true;

			fileName = file;
		}

        public Runner(double threshUp, double threshDown, string file, double dStarUp, double dStarDown)
		{
			deltaUp = threshUp; 
            deltaDown = threshDown;
			initalized = false;
			fileName = file;
			deltaStarUp = dStarUp; 
            deltaStarDown = dStarDown;
		}

        public int Run(TickPrice price)
		{
			if (price == null)
				return 0;

			if (!initalized)
			{
				type = -1; 
                osL = 0.0m; 
                initalized = true;

				prevExtreme = price.Mid; 
                prevExtremeTime = price.Time;

				prevDC = price.Mid; 
                prevDCTime = price.Time;

				extreme = price.Mid; 
                extremeTime = price.Time;
				reference = price.Mid;

				return 0;
			}

			if (type == -1)
			{
                if (Math.Log(decimal.ToDouble(price.Bid / extreme)) >= deltaUp)
				{
					prevExtreme = extreme;
					prevExtremeTime = extremeTime;
					
                    type = 1;

					extreme = price.Ask; 
                    extremeTime = price.Time;

					prevDC = price.Ask; 
                    prevDCTime = price.Time;

					reference = price.Ask;
					return 1;
				}
				if (price.Ask < extreme)
				{
					extreme = price.Ask;
					extremeTime = price.Time;

                    osL = (decimal)(-Math.Log(decimal.ToDouble(extreme / prevDC)) / deltaDown);

                    if (Math.Log(decimal.ToDouble(extreme / reference)) <= -deltaStarUp)
					{
						reference = extreme;
						return -2;
					}

					return 0;
				}
			}
			else if (type == 1)
			{
                if (Math.Log(decimal.ToDouble(price.Ask / extreme)) <= -deltaDown)
				{
					prevExtreme = extreme;
					prevExtremeTime = extremeTime;

					type = -1;

					extreme = price.Bid; 
                    extremeTime = price.Time;

					prevDC = price.Bid; 
                    prevDCTime = price.Time;

					reference = price.Bid;

					return -1;
				}
				if (price.Bid > extreme)
				{
					extreme = price.Bid;
					extremeTime = price.Time;

                    osL = (decimal)(Math.Log(decimal.ToDouble(extreme / prevDC)) / deltaUp);

                    if (Math.Log(decimal.ToDouble(extreme / reference)) >= deltaStarDown)
					{
						reference = extreme;
						return 2;
					}

					return 0;
				}
			}

			return 0;
		}

		public int Run(decimal price)
		{
			if (!initalized)
			{
				type = -1; 
                osL = 0.0m; 
                initalized = true;

				prevExtreme = price; 
                //prevExtremeTime = 0;

				prevDC = price; 
                //prevDCTime = 0;

				extreme = price; 
                //extremeTime = 0;

				reference = price;
				return 0;
			}

			if (type == -1)
			{
				if (price - extreme >= (decimal)deltaUp)
				{
					prevExtreme = extreme;
					prevExtremeTime = extremeTime;
					
                    type = 1;
					
                    extreme = price; 
                    //extremeTime = 0;

					prevDC = price; 
                    //prevDCTime = 0;

					reference = price;
					osL = 0.0m;

					return 1;
				}
				if (price < extreme)
				{
					extreme = price;
					//extremeTime = 0;

					osL = -(extreme - prevDC);

					if (extreme - reference <= (decimal)-deltaStarUp)
					{
						reference = extreme;
						return -2;
					}

					return 0;
				}
			}
			else if (type == 1)
			{
                if (price - extreme <= (decimal)-deltaDown)
				{
					prevExtreme = extreme; prevExtremeTime = extremeTime;
					type = -1;
					extreme = price; 
                    //extremeTime = 0;

					prevDC = price; 
                    //prevDCTime = 0;

					reference = price;
					osL = 0.0m;

					return 1;
				}
				if (price > extreme)
				{
					extreme = price; 
                    //extremeTime = 0;

					osL = (extreme - prevDC);
					if (extreme - reference >= (decimal)deltaStarDown)
					{
						reference = extreme;
						return 2;
					}
					return 0;
				}
			}
			return 0;
		}
	}
}
