using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort
{

    public class CoastlineTrader
    {
        private ILogger Logger = Logging.CreateLogger<CoastlineTrader>();

        decimal totalPosition;

        public decimal TotalPosition => totalPosition;

        LinkedList<decimal> prices;
        LinkedList<decimal> sizes;

        double profitTarget;
        decimal pnl, tempPnl;
        double deltaUp, deltaDown, deltaLiq, deltaOriginal;
        decimal shrinkFlong, shrinkFshort;

        double pnlPerc;

        OrderType ordersType;

        bool initalized;
        Runner runner;
        Runner[,] runnerG;

        double increaseLong, increaseShort;

        double lastPrice;

        double cashLimit;
        string fxRate;

        LocalLiquidity liquidity;

        public CoastlineTrader(double dOriginal, double dUp, double dDown, double profitT, string FxRate, OrderType ordersType)
        {
            prices = new LinkedList<decimal>();
            sizes = new LinkedList<decimal>();
            totalPosition = 0m;

            profitTarget = cashLimit = profitT;
            pnl = tempPnl = 0m;
            pnlPerc = 0.0;
            deltaOriginal = dOriginal;
            deltaUp = dUp; deltaDown = dDown;
            this.ordersType = ordersType;
            shrinkFlong = shrinkFshort = 1m;
            increaseLong = increaseShort = 0.0;

            fxRate = FxRate;
        }

        double computePnl(TickPrice price)
        {
            // compute PnL with current price
            return 0.0;
        }

        double computePnlLastPrice()
        {
            // compute PnL with last available price
            return 0.0;
        }
        double getPercPnl(TickPrice price)
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

        public bool RunPriceAsymm(TickPrice price, decimal oppositeInv)
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
                Logger.LogError("Didn't compute liquidity!");
            }

            if (tryToClose(price))
            { /* -- try to close position -- */
                Logger.LogInformation("Close");
                return true;
            }

            int @event = 0;


            decimal fraction = 1m;

            decimal size = (liquidity.Liq < 0.5 ? 0.5m : 1.0m);

            size = (liquidity.Liq < 0.1 ? 0.1m : size);

            if (ordersType == OrderType.Long)
            {
                @event = runner.Run(price);

                if (15m <= totalPosition && totalPosition < 30m)
                {
                    @event = runnerG[0, 0].Run(price);
                    runnerG[0, 1].Run(price);
                    fraction = 0.5m;
                }
                else if (totalPosition >= 30m)
                {
                    @event = runnerG[0, 1].Run(price);
                    runnerG[0, 0].Run(price);
                    fraction = 0.25m;
                }
                else
                {
                    runnerG[0, 0].Run(price);
                    runnerG[0, 1].Run(price);
                }

                if (@event < 0)
                {
                    if (totalPosition == 0m)
                    { // open long position
                        int sign = -runner.Type;
                        if (Math.Abs(oppositeInv) > 15m)
                        {
                            size = 1m;
                            if (Math.Abs(oppositeInv) > 30m)
                            {
                                size = 1m;
                            }
                        }

                        decimal sizeToAdd = sign * size;

                        totalPosition += sizeToAdd;

                        sizes.AddLast(sizeToAdd);

                        prices.AddLast(sign == 1 ? price.Ask : price.Bid);

                        assignCashTarget();
                        Logger.LogInformation("Open long");

                    }
                    else if (totalPosition > 0m)
                    { // increase long position (buy)
                        int sign = -runner.Type;

                        decimal sizeToAdd = sign * size * fraction * shrinkFlong;

                        if (sizeToAdd < 0m)
                        {
                            Logger.LogError("How did this happen! increase position but neg size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }

                        increaseLong += 1.0;
                        totalPosition += sizeToAdd;
                        sizes.AddLast(sizeToAdd);

                        prices.AddLast(sign == 1 ? price.Ask : price.Bid);
                        Logger.LogInformation("Cascade");
                    }
                }
                else if (@event > 0 && totalPosition > 0m)
                { // possibility to decrease long position only at intrinsic @events
                    decimal pricE = totalPosition > 0m ? price.Bid : price.Ask;

                    var currentPrice = prices.First;
                    var currentSize = sizes.First;

                    for (int i = 1; i < prices.Count; ++i)
                    {
                        currentPrice = currentPrice.Next;
                        currentSize = currentSize.Next;

                        double tempP = (totalPosition > 0m ? 
                                         Math.Log(decimal.ToDouble(pricE / currentPrice.Value)) : 
                                         Math.Log(decimal.ToDouble(currentPrice.Value / pricE)));

                        if (tempP >= (totalPosition > 0m ? deltaUp : deltaDown))
                        {
                            decimal addPnl = (pricE - prices.ElementAt(i)) * sizes.ElementAt(i);
                            if (addPnl < 0m)
                            {
                                Logger.LogInformation("Descascade with a loss: " + addPnl);
                            }
                            tempPnl += addPnl;
                            totalPosition -= sizes.ElementAt(i);


                            sizes.Remove(currentSize);
                            prices.Remove(currentPrice);

                            increaseLong += -1.0;
                            Logger.LogInformation("Decascade");
                        }
                    }
                }
            }
            else if (ordersType == OrderType.Short)
            {
                @event = runner.Run(price);
                if (-30m < totalPosition && totalPosition < -15m)
                {
                    @event = runnerG[1, 0].Run(price);
                    runnerG[1, 1].Run(price);
                    fraction = 0.5m;
                }
                else if (totalPosition <= -30m)
                {
                    @event = runnerG[1, 1].Run(price);
                    runnerG[1, 0].Run(price);
                    fraction = 0.25m;
                }
                else
                {
                    runnerG[1, 0].Run(price);
                    runnerG[1, 1].Run(price);
                }

                if (@event > 0)
                {
                    if (totalPosition == 0m)
                    { // open short position
                        int sign = -runner.Type;
                        if (Math.Abs(oppositeInv) > 15m)
                        {
                            size = 1m;
                            if (Math.Abs(oppositeInv) > 30m)
                            {
                                size = 1m;
                            }
                        }

                        decimal sizeToAdd = sign * size;

                        if (sizeToAdd > 0m)
                        {
                            Logger.LogError("How did this happen! increase position but pos size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }

                        totalPosition += sizeToAdd;

                        sizes.AddLast(sizeToAdd);

                        prices.AddLast(sign == 1 ? price.Bid : price.Ask);

                        Logger.LogInformation("Open short");
                        assignCashTarget();
                    }
                    else if (totalPosition < 0m)
                    {
                        int sign = -runner.Type;

                        decimal sizeToAdd = sign * size * fraction * shrinkFshort;

                        if (sizeToAdd > 0m)
                        {
                            Logger.LogError("How did this happen! increase position but pos size: " + sizeToAdd);
                            sizeToAdd = -sizeToAdd;
                        }

                        totalPosition += sizeToAdd;
                        sizes.AddLast(sizeToAdd);
                        increaseShort += 1.0;

                        prices.AddLast(sign == 1 ? price.Bid : price.Ask);

                        Logger.LogInformation("Cascade");
                    }
                }
                else if (@event < 0.0 && totalPosition < 0m)
                {
                    decimal pricE = totalPosition > 0m ? price.Bid : price.Ask;

                    var currentPrice = prices.First;
                    var currentSize = sizes.First;

                    for (int i = 1; i < prices.Count; ++i)
                    {
                        currentSize = currentSize.Next;
                        currentPrice = currentPrice.Next;

                        double tempP = (totalPosition > 0m ? 
                                        Math.Log(decimal.ToDouble(pricE / currentPrice.Value)) : 
                                        Math.Log(decimal.ToDouble(currentPrice.Value / pricE)));

                        if (tempP >= (totalPosition > 0m ? deltaUp : deltaDown))
                        {
                            decimal addPnl = (pricE - currentPrice.Value) * currentSize.Value;
                            if (addPnl < 0m)
                            {
                                Logger.LogInformation("Descascade with a loss: " + addPnl);
                            }

                            tempPnl += (pricE - currentPrice.Value) * currentSize.Value;
                            totalPosition -= currentSize.Value;

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
                Logger.LogError("Should never happen! " + ordersType);
            }
            return true;
        }
    }
}
