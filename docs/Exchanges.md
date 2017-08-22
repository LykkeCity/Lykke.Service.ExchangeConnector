# Exchanges

This is the list of exchanges (Trading Venues) and their possibilities. The Alpha Engine Trading Bot will integrate with these exchanges.


**Contents**:

 - [Kraken](#kraken)
 - [Bitstamp](#bitstamp)
 - [ICMCapital](#icmcapital)
 - [OANDA](#oanda)
 - [GDax](#gdax)


## Kraken

### API Info

Url: https://www.kraken.com

API documentation: https://www.kraken.com/en-us/help/api

**API call rate limits**:
We have safeguards in place to protect against abuse/DoS attacks as well as order book manipulation caused by the rapid placing and canceling of orders.

Every user of our API has a "call counter" which starts at 0.

Ledger/trade history calls increase the counter by 2.

Place/cancel order calls do not affect the counter.

All other API calls increase the counter by 1.

The user's counter is reduced every couple of seconds, and if the counter exceeds the user's maximum API access is suspended for 15 minutes. Tier 2 users have a maximum of 15 and their count gets reduced by 1 every 3 seconds. Tier 3 and 4 users have a maximum of 20; the count is reduced by 1 every 2 seconds for tier 3 users, and is reduced by 1 every 1 second for tier 4 users.

Although placing and cancelling orders does not increase the counter, there are separate limits in place to prevent order book manipulation. Only placing orders you intend to fill and keeping the rate down to 1 per second is generally enough to not hit this limit.

### Order Types

ordertype = order type:
    market
    limit (price = limit price)
    stop-loss (price = stop loss price)
    take-profit (price = take profit price)
    stop-loss-profit (price = stop loss price, price2 = take profit price)
    stop-loss-profit-limit (price = stop loss price, price2 = take profit price)
    stop-loss-limit (price = stop loss trigger price, price2 = triggered limit price)
    take-profit-limit (price = take profit trigger price, price2 = triggered limit price)
    trailing-stop (price = trailing stop offset)
    trailing-stop-limit (price = trailing stop offset, price2 = triggered limit offset)
    stop-loss-and-limit (price = stop loss price, price2 = limit price)
    settle-position

### Trading Pairs

 - DASHEUR, Base=DASH, Quote=ZEUR
 - DASHUSD, Base=DASH, Quote=ZUSD
 - DASHXBT, Base=DASH, Quote=XXBT
 - GNOETH, Base=GNO, Quote=XETH
 - GNOEUR, Base=GNO, Quote=ZEUR
 - GNOUSD, Base=GNO, Quote=ZUSD
 - GNOXBT, Base=GNO, Quote=XXBT
 - USDTUSD, Base=USDT, Quote=ZUSD
 - ETCETH, Base=XETC, Quote=XETH
 - ETCXBT, Base=XETC, Quote=XXBT
 - ETCEUR, Base=XETC, Quote=ZEUR
 - ETCUSD, Base=XETC, Quote=ZUSD
 - ETHXBT, Base=XETH, Quote=XXBT
 - ETHXBT.d, Base=XETH, Quote=XXBT
 - ETHCAD, Base=XETH, Quote=ZCAD
 - ETHCAD.d, Base=XETH, Quote=ZCAD
 - ETHEUR, Base=XETH, Quote=ZEUR
 - ETHEUR.d, Base=XETH, Quote=ZEUR
 - ETHGBP, Base=XETH, Quote=ZGBP
 - ETHGBP.d, Base=XETH, Quote=ZGBP
 - ETHJPY, Base=XETH, Quote=ZJPY
 - ETHJPY.d, Base=XETH, Quote=ZJPY
 - ETHUSD, Base=XETH, Quote=ZUSD
 - ETHUSD.d, Base=XETH, Quote=ZUSD
 - ICNETH, Base=XICN, Quote=XETH
 - ICNXBT, Base=XICN, Quote=XXBT
 - LTCXBT, Base=XLTC, Quote=XXBT
 - LTCEUR, Base=XLTC, Quote=ZEUR
 - LTCUSD, Base=XLTC, Quote=ZUSD
 - MLNETH, Base=XMLN, Quote=XETH
 - MLNXBT, Base=XMLN, Quote=XXBT
 - REPETH, Base=XREP, Quote=XETH
 - REPXBT, Base=XREP, Quote=XXBT
 - REPEUR, Base=XREP, Quote=ZEUR
 - REPUSD, Base=XREP, Quote=ZUSD
 - XBTCAD, Base=XXBT, Quote=ZCAD
 - XBTCAD.d, Base=XXBT, Quote=ZCAD
 - XBTEUR, Base=XXBT, Quote=ZEUR
 - XBTEUR.d, Base=XXBT, Quote=ZEUR
 - XBTGBP, Base=XXBT, Quote=ZGBP
 - XBTGBP.d, Base=XXBT, Quote=ZGBP
 - XBTJPY, Base=XXBT, Quote=ZJPY
 - XBTJPY.d, Base=XXBT, Quote=ZJPY
 - XBTUSD, Base=XXBT, Quote=ZUSD
 - XBTUSD.d, Base=XXBT, Quote=ZUSD
 - XDGXBT, Base=XXDG, Quote=XXBT
 - XLMXBT, Base=XXLM, Quote=XXBT
 - XLMEUR, Base=XXLM, Quote=ZEUR
 - XLMUSD, Base=XXLM, Quote=ZUSD
 - XMRXBT, Base=XXMR, Quote=XXBT
 - XMREUR, Base=XXMR, Quote=ZEUR
 - XMRUSD, Base=XXMR, Quote=ZUSD
 - XRPXBT, Base=XXRP, Quote=XXBT
 - XRPCAD, Base=XXRP, Quote=ZCAD
 - XRPEUR, Base=XXRP, Quote=ZEUR
 - XRPJPY, Base=XXRP, Quote=ZJPY
 - XRPUSD, Base=XXRP, Quote=ZUSD
 - ZECXBT, Base=XZEC, Quote=XXBT
 - ZECEUR, Base=XZEC, Quote=ZEUR
 - ZECUSD, Base=XZEC, Quote=ZUSD

### Minimal order amounts:
https://support.kraken.com/hc/en-us/articles/205893708-What-is-the-minimum-order-size-

 - Augur (REP): 0.3
 - Bitcoin (XBT): 0.002
 - Bitcoin Cash (BCH): 0.002
 - Dash (DASH): 0.03
 - Dogecoin (DOGE): 3000
 - EOS (EOS): 3
 - Ethereum (ETH): 0.02
 - Ethereum Classic (ETC): 0.3
 - Gnosis (GNO): 0.03
 - Iconomi (ICN): 2
 - Litecoin (LTC): 0.1
 - Melon (MLN): 0.1
 - Monero (XMR): 0.1
 - Ripple (XRP): 30
 - Stellar Lumens (XLM): 300
 - Zcash (ZEC): 0.03
 - Tether (USDT): 5

### Info Provided

Trades balance:

```
eb = equivalent balance (combined balance of all currencies)
tb = trade balance (combined balance of all equity currencies)
m = margin amount of open positions
n = unrealized net profit/loss of open positions
c = cost basis of open positions
v = current floating valuation of open positions
e = equity = trade balance + unrealized net profit/loss
mf = free margin = equity - initial margin (maximum margin available to open new positions)
ml = margin level = (equity / initial margin) * 100
```

#### Available methods:

 - Get account balance
 - Get trade balance
 - Get open orders
 - Get closed orders
 - Query orders info
 - Get trades history
 - Query trades info
 - Get open positions
 - Get ledgers info
 - Query ledgers
 - Get trade volume



## Bitstamp

Separate version of API implements FIX Protocol of version 4.4

There are HTTP and Sockets APIs as well.
Documentation: https://www.bitstamp.net/api/

### Limits

Do not make more than 600 requests per 10 minutes or we will ban your IP address. For real time data please refer to the websocket API.

### Info Provided

 - ACCOUNT BALANCE
    This API call is cached for 10 seconds. This call will be executed on the account (Sub or Main), to which the used API key is bound to.
 - USER TRANSACTIONS
 - OPEN ORDERS
    This API call is cached for 10 seconds. This call will be executed on the 
    account (Sub or Main), to which the used API key is bound to.
 - ORDER STATUS
 - CANCEL ORDER
 - CANCEL ALL ORDERS
 - BUY LIMIT ORDER
 - BUY MARKET ORDER
 - SELL LIMIT ORDER
 - SELL MARKET ORDER
 - 


### Order Types

 - Market
 - Limit

### Trading Pairs

 - btcusd
 - btceur
 - eurusd
 - xrpusd
 - xrpeur
 - xrpbtc


## ICMCapital

### Info

API implemented FIX protocol (http://fixprotocol.org) of version 4.4

**Operationg Hours**

5:00 EST Sunday - 17:00 EST Friday, No operation on Saturday

**Quantity**
ICM rejects orders which quantity is not divisible by Step. For FOREX currency pairs, Step is 1000 units of base currency. For Gold step is 1 ounce, for Silver step is 50 ounces.


### Order Types

 - Market. Order to buy or sell immediately executed against the best available price. Market order guarantees execution, while the execution price is unknown.
 - Limit. Order placed into the system to buy or sell at a specified price or better. Limit order guarantees execution price.
 - Stop order. An order to buy or sell becomes a market order when market rate surpasses a particular point (stop price). Buy Stop orders become market when ASK price reaches stop price. Sell Stop orders become market when BID price reaches stop price. Also referred to as a "stop-loss" order.
 - Stop-Limit order. Order combines the features of stop order with those of a limit order. When market rate surpasses a stop price, order becomes a limit order. That allow users to take influence on market slippage.


### Trading pairs

 - #AUS200 3
 - #BRENT  3
 - #CAC40  1
 - #DAX30  2
 - #EUSTX50    2
 - #FTSE100    1
 - #IBEX35 1
 - #JPN225 1
 - #S&P500 2
 - #WTI    3
 - AUD/CADm    5
 - AUD/CHFm    5
 - AUD/JPYm    3
 - AUD/NZDm    5
 - AUD/USDm    5
 - CAD/CHFm    5
 - CAD/JPYm    3
 - CHF/JPYm    3
 - EUR/AUDm    5
 - EUR/CADm    5
 - EUR/CHFm    5
 - EUR/DKKm    5
 - EUR/GBPm    5
 - EUR/HKDm    5
 - EUR/JPYm    3
 - EUR/MXNm    5
 - EUR/NOKm    5
 - EUR/NZDm    5
 - EUR/PLNm    5
 - EUR/SEKm    5
 - EUR/SGDm    5
 - EUR/TRYm    5
 - EUR/USDm    5
 - EUR/ZARm    5
 - GBP/AUDm    5
 - GBP/CADm    5
 - GBP/CHFm    5
 - GBP/DKKm    5
 - GBP/HKDm    5
 - GBP/JPYm    3
 - GBP/MXNm    5
 - GBP/NOKm    5
 - GBP/NZDm    5
 - GBP/PLNm    5
 - GBP/SEKm    5
 - GBP/SGDm    5
 - GBP/TRYm    5
 - GBP/USDm    5
 - GBP/ZARm    5
 - NZD/CADm    5
 - NZD/CHFm    5
 - NZD/JPYm    3
 - NZD/USDm    5
 - USD/CADm    5
 - USD/CHFm    5
 - USD/CNHm    5
 - USD/DKKm    5
 - USD/HKDm    5
 - USD/JPYm    3
 - USD/MXNm    5
 - USD/NOKm    5
 - USD/PLNm    5
 - USD/SEKm    5
 - USD/SGDm    5
 - USD/TRYm    5
 - USD/ZARm    5
 - XAG/USDm    5
 - XAU/USDm    3

### Supported operations:

Market Data Request (In)
Market Data Incremental Refresh (Out)
Market Data Request Reject (Out)
Security List Request (In)
Security List (Out)
New Order Single (In)
Execution Report (Out)
Order Replace Request (In)
Order Cancel Request (In)
Order Cancel Reject (Out)
Request for Positions (In)
Position Report (Out)
Order Status Request (In)



## OANDA

Url: https://www.oanda.com
API documentation: http://developer.oanda.com/rest-live/introduction/

### Trading Pairs

 - USD/MXN
 - GBP/USD
 - EUR/ZAR
 - AUD/JPY
 - Corn
 - EUR/CZK
 - GBP/CAD
 - USD/SAR
 - USD/HUF
 - EUR/NOK
 - Gold/HKD
 - Europe 50
 - SGD/CHF
 - China A50
 - USD/ZAR
 - EUR/USD
 - Silver/HKD
 - GBP/JPY
 - USD/TRY
 - EUR/CHF
 - GBP/ZAR
 - Gold/AUD
 - CAD/HKD
 - NZD/SGD
 - CAD/JPY
 - Silver/GBP
 - India 50
 - Copper
 - US Nas 100
 - EUR/TRY
 - USD/JPY
 - Silver
 - CHF/ZAR
 - NZD/USD
 - Natural Gas
 - Gold
 - Silver/JPY
 - Soybeans
 - US Wall St 30
 - AUD/USD
 - Silver/NZD
 - EUR/CAD
 - NZD/JPY
 - Platinum
 - EUR/NZD
 - GBP/PLN
 - USD/CNH
 - AUD/SGD
 - Germany 30
 - Gold/SGD
 - Wheat
 - Sugar
 - USD/SGD
 - EUR/SEK
 - US 5Y T-Note
 - Gold/EUR
 - Brent Crude Oil
 - NZD/CAD
 - USD/PLN
 - GBP/AUD
 - US T-Bond
 - Japan 225
 - Gold/Silver
 - Netherlands 25
 - AUD/NZD
 - US 10Y T-Note
 - ZAR/JPY
 - USD/DKK
 - USD/HKD
 - EUR/HKD
 - SGD/JPY
 - CAD/CHF
 - NZD/CHF
 - EUR/JPY
 - EUR/GBP
 - USD/CZK
 - GBP/NZD
 - NZD/HKD
 - Singapore 30
 - USD/NOK
 - Swiss 20
 - TRY/JPY
 - EUR/AUD
 - Silver/CHF
 - France 40
 - EUR/PLN
 - EUR/DKK
 - AUD/HKD
 - Gold/CAD
 - Silver/EUR
 - Silver/SGD
 - Taiwan Index
 - Gold/JPY
 - Gold/GBP
 - GBP/SGD
 - USD/SEK
 - CAD/SGD
 - Gold/CHF
 - GBP/HKD
 - Silver/AUD
 - CHF/HKD
 - USD/CAD
 - EUR/HUF
 - Gold/NZD
 - West Texas Oil
 - US SPX 500
 - EUR/SGD
 - UK 100
 - USD/THB
 - GBP/CHF
 - Australia 200
 - AUD/CHF
 - UK 10Y Gilt
 - USD/INR
 - Bund
 - SGD/HKD
 - Hong Kong 33
 - US 2Y T-Note
 - USD/CHF
 - AUD/CAD
 - US Russ 2000
 - Silver/CAD
 - Palladium
 - CHF/JPY
 - HKD/JPY


### Limits

Streams per user:    Aggregate of 20 connections
Rate limit (polling):    30


### Order Types

 - MarketOrder. A MarketOrder is an order that is filled immediately upon creation using the current market price.
 - LimitOrder. A LimitOrder is an order that is created with a price threshold, and will only be filled by a price that is equal to or better than the threshold.
 - StopOrder. A StopOrder is an order that is created with a price threshold, and will only be filled by a price that is equal to or worse than the threshold.
 - MarketIfTouchedOrder. A MarketIfTouchedOrder is an order that is created with a price threshold, and will only be filled by a market price that is touches or crosses the threshold.
 - TakeProfitOrder. A TakeProfitOrder is an order that is linked to an open Trade and created with a price threshold. The Order will be filled (closing the Trade) by the first price that is equal to or better than the threshold. A TakeProfitOrder cannot be used to open a new Position.
 - StopLossOrder. A StopLossOrder is an order that is linked to an open Trade and created with a price threshold. The Order will be filled (closing the Trade) by the first price that is equal to or worse than the threshold. A StopLossOrder cannot be used to open a new Position.
 - TrailingStopLossOrder. A TrailingStopLossOrder is an order that is linked to an open Trade and created with a price distance. The price distance is used to calculate a trailing stop value for the order that is in the losing direction from the market price at the time of the order’s creation. The trailing stop value will follow the market price as it moves in the winning direction, and the order will filled (closing the Trade) by the first price that is equal to or worse than the trailing stop value. A TrailingStopLossOrder cannot be used to open a new Position.


The type of the order ‘limit’, ‘stop’, ‘marketIfTouched’ or ‘market’.

```
Input Data Parameters (inside body)

instrument:* Required Instrument to open the order on.

units: Required The number of units to open order for.

side: Required Direction of the order, either ‘buy’ or ‘sell’.

type: Required The type of the order ‘limit’, ‘stop’, ‘marketIfTouched’ or ‘market’.

expiry: Required If order type is ‘limit’, ‘stop’, or ‘marketIfTouched’. The order expiration time in UTC. The value specified must be in a valid datetime format.

price: Required If order type is ‘limit’, ‘stop’, or ‘marketIfTouched’. The price where the order is set to trigger at.

lowerBound: Optional The minimum execution price.

upperBound: Optional The maximum execution price.

stopLoss: Optional The stop loss price.

takeProfit: Optional The take profit price.

trailingStop: Optional The trailing stop distance in pips, up to one decimal place.
```


## GDax

http://gdax.com

Socket api with prices stream
FIX or REST API for orders
0% fees for market makers

### API Limits

#### REST API

##### PUBLIC ENDPOINTS

We throttle public endpoints by IP: 3 requests per second, up to 6 requests per second in bursts.

##### PRIVATE ENDPOINTS

We throttle private endpoints by user ID: 5 requests per second, up to 10 requests per second in bursts.

##### FINANCIAL INFORMATION EXCHANGE API

The FIX API throttles each command type (eg.: NewOrderSingle, OrderCancelRequest) to 30 commands per second.


### Trading pairs

For Europe:

 - BTC/EUR
 - ETH/EUR
 - ETH/BTC
 - LTC/EUR
 - LTC/BTC

### Margin

With some margin in the margin profile’s balance you can place orders that draw funding. You can draw upto 2x your margin. Your margin is your equity in the margin profile.

### Order Types

- Limit
- Market
- Stop
