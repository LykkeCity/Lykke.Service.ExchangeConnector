using System;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort
{

    public class LocalLiquidity
    {
        double deltaUp, deltaDown;
        double delta;
        double extreme, dStar, reference;
        int type;
        bool initalized;

        double surp, upSurp, downSurp;
        double liq, upLiq, downLiq;
        double alpha, alphaWeight;
        double H1, H2;

        public double Liq => liq;

        public LocalLiquidity(double d, double dUp, double dDown, double dS, double a)
        {
            type = -1; 
            deltaUp = dUp; 
            deltaDown = dDown; 
            dStar = dS; 
            delta = d;

            initalized = false;
            alpha = a;
            alphaWeight = Math.Exp(-2.0 / (a + 1.0));
            ComputeH1H2exp(dS);
        }

        public LocalLiquidity(double d, double dUp, double dDown, TickPrice price, double dS, double a)
        {
            deltaUp = dUp; 
            deltaDown = dDown; 
            delta = d;

            type = -1;
            extreme = reference = (double)price.Mid;
            dStar = dS;
            initalized = true;
            alpha = a;
            alphaWeight = Math.Exp(-2.0 / (a + 1.0));
            ComputeH1H2exp(dS);
        }

        bool ComputeH1H2exp(double dS)
        {
            H1 = -Math.Exp(-dStar / delta) * Math.Log(Math.Exp(-dStar / delta)) - (1.0 - Math.Exp(-dStar / delta)) * Math.Log(1.0 - Math.Exp(-dStar / delta));
            H2 = Math.Exp(-dStar / delta) * Math.Pow(Math.Log(Math.Exp(-dStar / delta)), 2.0) - (1.0 - Math.Exp(-dStar / delta)) * Math.Pow(Math.Log(1.0 - Math.Exp(-dStar / delta)), 2.0) - H1 * H1;
            return true;
        }
        // another implementation of the CNDF for a standard normal: N(0,1)
        double CumNorm(double x)
        {
            // protect against overflow
            if (x > 6.0)
                return 1.0;
            if (x < -6.0)
                return 0.0;

            double b1 = 0.31938153;
            double b2 = -0.356563782;
            double b3 = 1.781477937;
            double b4 = -1.821255978;
            double b5 = 1.330274429;
            double p = 0.2316419;
            double c2 = 0.3989423;

            double a = Math.Abs(x);
            double t = 1.0 / (1.0 + a * p);
            double b = c2 * Math.Exp((-x) * (x / 2.0));
            double n = ((((b5 * t + b4) * t + b3) * t + b2) * t + b1) * t;
            n = 1.0 - b * n;

            if (x < 0.0)
                n = 1.0 - n;

            return n;
        }

        int Run(TickPrice price)
        {
            if (price == null)
                return 0;

            if (!initalized)
            {
                type = -1; initalized = true;
                extreme = reference = (double)price.Mid;
                return 0;
            }

            if (type == -1)
            {
                if (Math.Log((double)price.Bid / extreme) >= deltaUp)
                {
                    type = 1;
                    extreme = (double)price.Ask;
                    reference = (double)price.Ask;
                    return 1;
                }
                if ((double)price.Ask < extreme)
                {
                    extreme = (double)price.Ask;
                }
                if (Math.Log(reference / extreme) >= dStar)
                {
                    reference = extreme;
                    return 2;
                }
            }
            else if (type == 1)
            {
                if (Math.Log((double)price.Ask / extreme) <= -deltaDown)
                {
                    type = -1;
                    extreme = (double)price.Bid;
                    reference = (double)price.Bid;
                    return -1;
                }
                if ((double)price.Bid > extreme)
                {
                    extreme = (double)price.Bid;
                }
                if (Math.Log(reference / extreme) <= -dStar)
                {
                    reference = extreme;
                    return -2;
                }
            }
            return 0;
        }

        public bool Computation(TickPrice price)
        {
            if (price == null)
                return false;

            int @event = Run(price);

            if (@event != 0)
            {
                surp = alphaWeight * (Math.Abs(@event) == 1 ? 0.08338161 : 2.525729) + (1.0 - alphaWeight) * surp;

                if (@event > 0)
                { // down moves
                    downSurp = alphaWeight * (@event == 1 ? 0.08338161 : 2.525729) + (1.0 - alphaWeight) * downSurp;
                }
                else if (@event < 0)
                { // up moves
                    upSurp = alphaWeight * (@event == -1 ? 0.08338161 : 2.525729) + (1.0 - alphaWeight) * upSurp;
                }

                liq = 1.0 - CumNorm(Math.Sqrt(alpha) * (surp - H1) / Math.Sqrt(H2));
                upLiq = 1.0 - CumNorm(Math.Sqrt(alpha) * (upSurp - H1) / Math.Sqrt(H2));
                downLiq = 1.0 - CumNorm(Math.Sqrt(alpha) * (downSurp - H1) / Math.Sqrt(H2));
            }

            return true;
        }
    }
}
