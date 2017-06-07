using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Trading;

namespace TheAlphaEngine.NET
{

    public class CoastlineTrader
    {
        private ILogger Logger = Logging.CreateLogger<CoastlineTrader>();

        double tP; /* -- total position -- */

        public double TotalPosition => tP;

        LinkedList<Double> prices;
        LinkedList<Double> sizes;

        double profitTarget;
        double pnl, tempPnl;
        double deltaUp, deltaDown, deltaLiq, deltaOriginal;
        double shrinkFlong, shrinkFshort;

        double pnlPerc;

        int longShort;

        bool initalized;
        Runner runner;
        Runner[,] runnerG;

        double increaseLong, increaseShort;

        double lastPrice;

        double cashLimit;
        string fxRate;

        LocalLiquidity liquidity;

        public CoastlineTrader(double dOriginal, double dUp, double dDown, double profitT, string FxRate, int lS)
        {
            prices = new LinkedList<Double>();
            sizes = new LinkedList<Double>();
            tP = 0.0; /* -- total position -- */

            profitTarget = cashLimit = profitT;
            pnl = tempPnl = pnlPerc = 0.0;
            deltaOriginal = dOriginal;
            deltaUp = dUp; deltaDown = dDown;
            longShort = lS; // 1 for only longs, -1 for only shorts
            shrinkFlong = shrinkFshort = 1.0;
            increaseLong = increaseShort = 0.0;

            fxRate = FxRate;
        }

        double computePnl(PriceFeedData price)
        {
            // compute PnL with current price
            return 0.0;
        }

        double computePnlLastPrice()
        {
            // compute PnL with last available price
            return 0.0;
        }
        double getPercPnl(PriceFeedData price)
        {
            // percentage PnL
            return 0.0;
        }

        bool tryToClose(TickPrice price)
        {
            // PnL target hit implementation
            return false;
        }

        bool assignCashTarget()
        {
            // implement
            return true;
        }

        public bool RunPriceAsymm(TickPrice price, double oppositeInv)
        {
            if (!initalized)
            {
                runner = new Runner(deltaUp, deltaDown, price, fxRate, deltaUp, deltaDown);

                runnerG = new Runner[2, 2];

                runnerG[0, 0] = new Runner(0.75 * deltaUp, 1.50 * deltaDown, price, fxRate, 0.75 * deltaUp, 0.75 * deltaUp);
                runnerG[0, 1] = new Runner(0.50 * deltaUp, 2.00 * deltaDown, price, fxRate, 0.50 * deltaUp, 0.50 * deltaUp);

                runnerG[1, 0] = new Runner(1.50 * deltaUp, 0.75 * deltaDown, price, fxRate, 0.75 * deltaDown, 0.75 * deltaDown);
                runnerG[1, 1] = new Runner(2.00 * deltaUp, 0.50 * deltaDown, price, fxRate, 0.50 * deltaDown, 0.50 * deltaDown);

                liquidity = new LocalLiquidity(deltaOriginal, deltaUp, deltaDown, price, deltaOriginal * 2.525729, 50.0);
                initalized = true;
            }

            if (!liquidity.Computation(price))
            {
                Console.WriteLine("Didn't compute liquidity!");
            }

            if (tryToClose(price))
            { /* -- try to close position -- */
                Logger.LogInformation("Close");
                return true;
            }

            int @event = 0;


            double fraction = 1.0;
            double size = (liquidity.Liq < 0.5 ? 0.5 : 1.0);
            size = (liquidity.Liq < 0.1 ? 0.1 : size);

            if (longShort == 1)
            { // long positions only
                @event = runner.Run(price);

                if (15.0 <= tP && tP < 30.0)
                {
                    @event = runnerG[0, 0].Run(price);
                    runnerG[0, 1].Run(price);
                    fraction = 0.5;
                }
                else if (tP >= 30.0)
                {
                    @event = runnerG[0, 1].Run(price);
                    runnerG[0, 0].Run(price);
                    fraction = 0.25;
                }
                else
                {
                    runnerG[0, 0].Run(price);
                    runnerG[0, 1].Run(price);
                }

                if (@event < 0)
                {
                    if (tP == 0.0)
                    { // open long position
                        int sign = -runner.Type;
                        if (Math.Abs(oppositeInv) > 15.0)
                        {
                            size = 1.0;
                            if (Math.Abs(oppositeInv) > 30.0)
                            {
                                size = 1.0;
                            }
                        }
                        double sizeToAdd = sign * size;
                        tP += sizeToAdd;
                        sizes.AddLast(sizeToAdd);

                        prices.AddLast((double)(sign == 1 ? price.Ask : price.Bid));
                        assignCashTarget();
                        Logger.LogInformation("Open long");

                    }
                    else if (tP > 0.0)
                    { // increase long position (buy)
                        int sign = -runner.Type;
                        double sizeToAdd = sign * size * fraction * shrinkFlong;
                        if (sizeToAdd < 0.0)
                        {
                            Logger.LogError("How did this happen! increase position but neg size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }
                        increaseLong += 1.0;
                        tP += sizeToAdd;
                        sizes.AddLast(sizeToAdd);

                        prices.AddLast((double)(sign == 1 ? price.Ask : price.Bid));
                        Logger.LogInformation("Cascade");
                    }
                }
                else if (@event > 0 && tP > 0.0)
                { // possibility to decrease long position only at intrinsic @events
                    double pricE = (double)(tP > 0.0 ? price.Bid : price.Ask);

                    var currentPrice = prices.First;
                    var currentSize = sizes.First;

                    for (int i = 1; i < prices.Count; ++i)
                    {
                        currentPrice = currentPrice.Next;
                        currentSize = currentSize.Next;

                        double tempP = (tP > 0.0 ? Math.Log(pricE / currentPrice.Value) : Math.Log(currentPrice.Value / pricE));
                        if (tempP >= (tP > 0.0 ? deltaUp : deltaDown))
                        {
                            double addPnl = (pricE - prices.ElementAt(i)) * sizes.ElementAt(i);
                            if (addPnl < 0.0)
                            {
                                Logger.LogInformation("Descascade with a loss: " + addPnl);
                            }
                            tempPnl += addPnl;
                            tP -= sizes.ElementAt(i);


                            sizes.Remove(currentSize);
                            prices.Remove(currentPrice);

                            increaseLong += -1.0;
                            Logger.LogInformation("Decascade");
                        }
                    }
                }
            }
            else if (longShort == -1)
            { // short positions only
                @event = runner.Run(price);
                if (-30.0 < tP && tP < -15.0)
                {
                    @event = runnerG[1, 0].Run(price);
                    runnerG[1, 1].Run(price);
                    fraction = 0.5;
                }
                else if (tP <= -30.0)
                {
                    @event = runnerG[1, 1].Run(price);
                    runnerG[1, 0].Run(price);
                    fraction = 0.25;
                }
                else
                {
                    runnerG[1, 0].Run(price);
                    runnerG[1, 1].Run(price);
                }

                if (@event > 0)
                {
                    if (tP == 0.0)
                    { // open short position
                        int sign = -runner.Type;
                        if (Math.Abs(oppositeInv) > 15.0)
                        {
                            size = 1.0;
                            if (Math.Abs(oppositeInv) > 30.0)
                            {
                                size = 1.0;
                            }
                        }
                        double sizeToAdd = sign * size;
                        if (sizeToAdd > 0.0)
                        {
                            Logger.LogError("How did this happen! increase position but pos size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }
                        tP += sizeToAdd;
                        sizes.AddLast(sizeToAdd);

                        prices.AddLast((double)(sign == 1 ? price.Bid : price.Ask));

                        Logger.LogInformation("Open short");
                        assignCashTarget();
                    }
                    else if (tP < 0.0)
                    {
                        int sign = -runner.Type;
                        double sizeToAdd = sign * size * fraction * shrinkFshort;
                        if (sizeToAdd > 0.0)
                        {
                            Logger.LogError("How did this happen! increase position but pos size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }

                        tP += sizeToAdd;
                        sizes.AddLast(sizeToAdd);
                        increaseShort += 1.0;

                        prices.AddLast((double)(sign == 1 ? price.Bid : price.Ask));
                        Logger.LogInformation("Cascade");
                    }
                }
                else if (@event < 0.0 && tP < 0.0)
                {
                    double pricE = (double)(tP > 0.0 ? price.Bid : price.Ask);

                    var currentPrice = prices.First;
                    var currentSize = sizes.First;

                    for (int i = 1; i < prices.Count; ++i)
                    {
                        currentSize = currentSize.Next;
                        currentPrice = currentPrice.Next;

                        double tempP = (tP > 0.0 ? Math.Log(pricE / currentPrice.Value) : Math.Log(currentPrice.Value / pricE));
                        if (tempP >= (tP > 0.0 ? deltaUp : deltaDown))
                        {
                            double addPnl = (pricE - currentPrice.Value) * currentSize.Value;
                            if (addPnl < 0.0)
                            {
                                Logger.LogInformation("Descascade with a loss: " + addPnl);
                            }

                            tempPnl += (pricE - currentPrice.Value) * currentSize.Value;
                            tP -= currentSize.Value;

                            sizes.Remove(currentSize);
                            prices.Remove(currentPrice);

                            increaseShort += -1.0;
                            Logger.LogInformation("Decascade");
                        }
                    }
                }
            }
            else
            {
                Logger.LogError("Should never happen! " + longShort);
            }
            return true;
        }
    }
}
