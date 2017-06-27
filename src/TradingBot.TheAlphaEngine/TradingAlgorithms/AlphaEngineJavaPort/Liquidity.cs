using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort
{

    public class Liquidity
    {
        
        public class Runner
        {
            public double prevDC;
            public double extreme;
            
            public double deltaUp;
            public double deltaDown;
            public int type;
            public bool initalized;
            
            public String fileName;
            
            public Runner(double threshUp, double threshDown, TickPrice price, string file)
            {
                prevDC = (double)price.Mid; 
                extreme = (double)price.Mid; 
                
                type = -1; 
                deltaUp = threshUp; 
                deltaDown = threshDown;
                initalized = true;

                fileName = file;
            }

            public Runner(double threshUp, double threshDown, double price, string file)
            {
                prevDC = price; 
                extreme = price; 
                
                type = -1; 
                deltaUp = threshUp; 
                deltaDown = threshDown;  
                initalized = true;

                fileName = file;
            }
            
            public Runner(double threshUp, double threshDown, String file)
            {
                deltaUp = threshUp; 
                deltaDown = threshDown;
                initalized = false;
                fileName = file;
            }
            
            public int run(TickPrice price)
            {
                if( price == null )
                    return 0;
                
                if( !initalized )
                {
                    type = -1; 
                    initalized = true;
                    prevDC = (double)price.Mid;
                    extreme = (double)price.Mid;

                    return 0;
                }
                
                if( type == -1 )
                {
                    if( Math.Log((double)price.Bid/extreme) >= deltaUp )
                    {
                        type = 1;

                        extreme = (double)price.Ask; 
                        prevDC = (double)price.Ask;   

                        return 1;
                    }
                    if( (double)price.Ask < extreme )
                    {
                        extreme = (double)price.Ask;
                        return 0;
                    }
                }
                else if( type == 1 )
                {
                    if( Math.Log((double)price.Ask/extreme) <= -deltaDown ){
                        type = -1;

                        extreme = (double)price.Bid;
                        prevDC = (double)price.Bid; 

                        return -1;
                    }
                    if( (double)price.Bid > extreme ){
                        extreme = (double)price.Bid; 
                        return 0;
                    }
                }
                return 0;
            }
            
            public int run(double price)
            {
                if( !initalized )
                {
                    type = -1; 
                    initalized = true;

                    prevDC = price;
                    extreme = price; 

                    return 0;
                }
                
                if( type == -1 )
                {
                    if( price - extreme >= deltaUp )
                    {
                        type = 1;
                        extreme = price; 
                        prevDC = price;
                        return 1;
                    }
                    if( price < extreme )
                    {
                        extreme = price;
                        return 0;
                    }
                }
                else if( type == 1 )
                {
                    if( price - extreme <= -deltaDown )
                    {
                        type = -1;
                        extreme = price; 
                        prevDC = price; ;
                        return 1;
                    }
                    if( price > extreme )
                    {
                        extreme = price;
                        return 0;
                    }
                }
                return 0;
            }
        }
        
        public Runner[] runner;
        int[] prevState;
        double surp = 0.0, dSurp = 0.0, uSurp = 0.0;
        double liquidity, liquidityUp, liquidityDown; 
        double liqEMA;
        double upLiq, downLiq, diffLiq, diffRaw;
        double H1 = 0.0, H2 = 0.0;
        double d1 = 0.0, d2 = 0.0;
        double alpha, alphaWeight;

        LinkedList<Double> mySurprise, downSurprise, upSurprise;
        
        public Liquidity()
        {
            
        }

        public Liquidity(TickPrice price, double delta1, double delta2, int lgt)
        {
            double prob = Math.Exp(-1.0);

            H1 = -(prob*Math.Log(prob) + (1.0 - prob)*Math.Log(1.0 - prob));
            H2 = prob*Math.Pow(Math.Log(prob), 2.0) + (1.0 - prob)*Math.Pow(Math.Log(1.0 - prob), 2.0) - H1*H1;

            runner = new Runner[lgt];
            prevState = new int[lgt];
            d1 = delta1; d2 = delta2;
    
            getH1nH2(); //skip computation and assign!
            
            runner = new Runner[lgt];
            prevState = new int[lgt];
            
            for( int i = 0; i < runner.Length; ++i )
            {
                runner[i] = new Runner(0.025/100.0 + 0.05/100.0*(double)i, 0.025/100.0 + 0.05/100.0*(double)i, price, "JustFake");
                runner[i].type = (i%2 == 0 ? 1 : -1);
                prevState[i] = (runner[i].type == 1 ? 1 : 0);
            }

            surp = H1; dSurp = H1; uSurp = H1;
            liquidity = 0.5; 
            liqEMA = 0.5;
            
            mySurprise = new LinkedList<Double>();
            downSurprise = new LinkedList<Double>();
            upSurprise = new LinkedList<Double>();

            for( int i = 0; i < 100; ++i ){
                mySurprise.AddLast(H1);
                downSurprise.AddLast(H1);
                upSurprise.AddLast(H1);
            }
            
            //computeLiquidity();
            
            downLiq = 0.5; 
            upLiq = 0.5; 
            diffLiq = 0.5; 
            diffRaw = 0.0;
            alpha = 2.0/(100.0 + 1.0); 
            alphaWeight = Math.Exp(-alpha); 
        }
        
        public void getH1nH2(){
            double H1temp = 0.0; 
            double H2temp = 0.0;
            double price = 0.0; 
            alpha = 2.0/(100.0 + 1.0);

            alphaWeight = Math.Exp(-alpha);

            runner = new Runner[runner.Length];

            for( int i = 0; i < runner.Length; ++i )
            {
                runner[i] = new Runner(0.025/100.0 + 0.05/100.0*(double)i, 0.025/100.0 + 0.05/100.0*(double)i, price, "JustFake");
                runner[i].type = (i%2 == 0 ? 1 : -1);
                prevState[i] = (runner[i].type == 1 ? 1 : 0);
            }
            
            double total1 = 0.0, total2 = 0.0;
            Random rand = new Random(1);

            double dt = 1.0/Math.Sqrt(1000000.0);
            double sigma = 0.25; // 25%

            for( int i = 0; i < 100000000; ++i )
            {
                price += sigma * dt * rand.NextDouble(); //nextGaussian();

                for( int j= 0; j < runner.Length; ++j )
                {
                    if( Math.Abs(runner[j].run(price)) == 1 )
                    { // this is OK for simulated prices
                        double myProbs = getProbs(j);
                        total1 = total1*alphaWeight + (1.0 - alphaWeight)*(-Math.Log(myProbs));
                        total2 = total2*alphaWeight + (1.0 - alphaWeight)*Math.Pow(Math.Log(myProbs), 2.0);
                        
                        //H1temp = (H1temp*total + -Math.log(myProbs))/(total + 1.0);
                        //H2temp = (H2temp*total + Math.pow(Math.log(myProbs), 2.0))/(total + 1.0); 
                        //total += 1.0;
                    }
                }
            }
            H1 = total1;
            H2 = total2 - H1*H1;
            Console.WriteLine("H1:" + H1 + " H2:" + H2);
        }
        
        public bool Trigger(TickPrice price)
        {
            // -- update values -- 
            bool doComp = false;
            for( int i = 0; i < runner.Length; ++i )
            {
                int value = runner[i].run(price);
                if( Math.Abs(value) == 1 )
                {
                    //double alpha = 2.0/(100.0 + 1.0);
                    double myProbs = getProbs(i);
                    surp = surp*alphaWeight + (1.0 - alphaWeight)*(-Math.Log(myProbs));

                    mySurprise.RemoveFirst(); 
                    mySurprise.AddLast(-Math.Log(myProbs));

                    if( runner[i].type == -1 )
                    {
                        dSurp = dSurp*alphaWeight + (1.0 - alphaWeight)*(-Math.Log(myProbs));
                        downSurprise.RemoveFirst(); 
                        downSurprise.AddLast(-Math.Log(myProbs));
                    }
                    else if( runner[i].type == 1 )
                    {
                        uSurp = uSurp*alphaWeight + (1.0 - alphaWeight)*(-Math.Log(myProbs));
                        upSurprise.RemoveFirst(); 
                        upSurprise.AddLast(-Math.Log(myProbs));
                    }

                    doComp = true;
                }
            }
            if( doComp )
            {
                liqEMA = (1.0 - CumNorm(Math.Sqrt(100.0)*(surp - H1)/Math.Sqrt(H2)));
                upLiq = (1.0 - CumNorm(Math.Sqrt(100.0)*(uSurp - H1)/Math.Sqrt(H2)));
                downLiq =  (1.0 - CumNorm(Math.Sqrt(100.0)*(dSurp - H1)/Math.Sqrt(H2)));
                diffLiq = CumNorm(Math.Sqrt(100.0)*(uSurp - dSurp)/Math.Sqrt(H2));
                diffRaw = Math.Sqrt(100.0)*(uSurp-dSurp)/Math.Sqrt(H2);
                //computeLiquidity();
            }
            return doComp;
        }
        
        public double getProbs(int i)
        {
            int where = -1;
            for( int j = 1; j < prevState.Length; ++j )
            {
                if( prevState[j] != prevState[0] ){
                    where = j;
                    break;
                }
            }
            if( i > 0 && where != i )
            {
                //System.out.println("This should not happen! " + where);
            }
            prevState[i] = (prevState[i] == 1 ? 0 : 1);
            
            if( where == 1 )
            {
                if( i > 0 )
                {
                    return Math.Exp(-(runner[1].deltaDown - runner[0].deltaDown)/runner[0].deltaDown);
                }else
                {
                    return (1.0 - Math.Exp(-(runner[1].deltaDown - runner[0].deltaDown)/runner[0].deltaDown));
                }
            }
            else if( where > 1 )
            {
                double numerator = 0.0;
                for( int k = 1; k <= where; ++k )
                {
                    numerator -= (runner[k].deltaDown - runner[k-1].deltaDown)/runner[k-1].deltaDown;
                }
                numerator = Math.Exp(numerator);
                double denominator = 0.0;

                for( int k = 1; k <= where - 1; ++k )
                {
                    double secVal = 0.0;
                    for( int j  = k+1; j <= where; ++j )
                    {
                        secVal -=  (runner[j].deltaDown - runner[j-1].deltaDown)/runner[j-1].deltaDown;
                    }
                    denominator += (1.0 - Math.Exp(-(runner[k].deltaDown - runner[k-1].deltaDown)/runner[k-1].deltaDown))*Math.Exp(secVal);
                }
                if( i > 0 )
                {
                    return numerator/(1.0 - denominator);
                }
                else
                {
                    return (1.0 - numerator/(1.0 - denominator));
                }
            }else{
                return 1.0;
            }
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
            double b = c2*Math.Exp((-x)*(x/2.0));
            double n = ((((b5*t+b4)*t+b3)*t+b2)*t+b1)*t;
            n = 1.0-b*n;
            
            if ( x < 0.0 )
                n = 1.0 - n;

            return n;
        }
        
        
        public bool computeLiquidity(long deltaT)
        {
            double surpT = 0.0;
            double downSurp = 0.0, upSurp = 0.0;
            
            for( int i = 0; i < mySurprise.Count; ++i )
            {
                surpT += mySurprise.ElementAt(i);
                downSurp += downSurprise.ElementAt(i);
                upSurp += upSurprise.ElementAt(i);
            }
            
            liquidity = 1.0 - CumNorm((surpT - H1*mySurprise.Count/Math.Sqrt(H2*mySurprise.Count)));
            liquidityDown = 1.0 - CumNorm((downSurp - H1*downSurprise.Count)/Math.Sqrt(H2*downSurprise.Count));
            liquidityUp = 1.0 - CumNorm((upSurp - H1*upSurprise.Count/Math.Sqrt(H2*upSurprise.Count)));
            
            return true;
        }
    };
    
}
