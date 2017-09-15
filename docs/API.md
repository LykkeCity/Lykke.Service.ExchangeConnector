# ExchangeConnector API

ExchangeConnector provides an HTTP API for interaction with connected exchanges, such as placing new orders, check status of the earlier placed orders and so on.

## Getting list of connected exchanges

**URL**: `GET /api/v1/exchanges`

### Response

Response will contain a list of all available exchanges. Example of the response:

```json
[
    "icm",
    "kraken",
    "bitstamp"
]
```

## Getting list of available instruments for specific exchange

**URL**: `GET /api/v1/exchanges/{exchangeName}`

### Response

Response will contain a list of all available instruments. Exmaple of the response:

```json
[
    {
        "name": "EURUSD",
        "exchange": "icm"
    },
    {
        "name": "EURCHF",
        "exchange": "icm"
    }
]
```


## Placing a new order

**URL**: `POST /api/v1/orders/{exchangeName}`

### Market order example

```json
{
    "instrument": "EURUSD",
    "id": 1,
    "volume": 1000,
    "tradeType": "sell",
    "orderType": "market"
}
```

### Limit order example


```json
{
    "instrument": "EURUSD",
    "id": 1,
    "volume": 1000,
    "price": 12.1234,
    "tradeType": "sell",
    "orderType": "limit"
}
```

### Possible values

 - **`instrument`**: available for the exchange value of type `string`
 - **`id`**: unique order id of type `long`
 - **`volume`**: volume of the trade, the type is `decimal` with dot as a separator
 - **`price`**: required for limit order only. Value of type `decimal` with dot a separator
 - **`tradeType`**: `"buy"` or `0` for buy order, `"sell"` or `1` for sell order
 - **`orderType`**: `"market"` or `0` for market order, `"limit"` or `1` for limit order


### Response

If the order is successfully placed to the exchange, response will have status code `201 (Created)` and contains info about the order:

```json
{
    "instrument": {
        "name": "EURUSD",
        "exchange": "icm"
    },
    "type": 1,
    "time": "2017-08-08T17:31:42.565Z",
    "price": 0,
    "volume": 1000,
    "fee": 0,
    "orderId": 1,
    "status": 4
}
```

Possible values of **`executionStatus`**: 
 - `0` = `Fill`, 
 - `1` = `PartialFill`, 
 - `2` = `Cancelled`, 
 - `3` = `Rejected`, 
 - `4` = `New` or 
 - `5` = `Pending`.

Response also contains a `Location` header with the location of placed order, such as `/api/v1/orders/icm/EURUSD/2`. This is a URL for getting information about the order.

If there is an error, response will have status code `400 (Bad Request)`  and contains info about the error:

```json

```

### Signing of the request

The request for placing a new order must be signed with the API KEY. Details of the signig and example code is above.

The string to sign for request is a concatenation of the values: `Id`, `Instrument`, `TradeType`, `OrderType`, `Price` and `Volume` in the following format:

```c#
$"{DateTime:s}{Id}{Instrument}{TradeType}{OrderType}{Price:0.0000}{Volume:0.0000}"
```

Where `{DateTime:s}` produce date and time in format `YYYY-MM-DDTHH:mm:ss`, for example: `2017-08-13T12:00:00`.

## Getting info about earlier placed order

**URL**: `GET /api/v1/orders/{exchangeName}/{instrument}/{orderId}`

### Response

```json
{
    "instrument": {
        "name": "EURUSD",
        "exchange": "icm"
    },
    "type": 1,
    "time": "2017-08-09T08:26:29.915Z",
    "price": 1.5,
    "volume": 1000,
    "fee": 0,
    "orderId": 1,
    "status": 5
}
```


## Cancelling previously added order

**URL**: `DELETE /api/v1/orders/{exchangeName}`

body:

```json
{
    "instrument": "EURUSD",
    "id": 1,
    "volume": 1000,
    "price": 12.1234,
    "tradeType": "sell",
    "orderType": "limit"
}
```

## API Authorization

API calls are authorized with API KEY. Every request must be signed with the key. The request sign must be placed into `Authorization` header of the request.

Example code of signing the request for placing a new order:

```c#
var apiKey = "{API_KEY}";
var stringToSign = $"{DateTime:s}{Id}{Instrument}{TradeType}{OrderType}{Price:0.0000}{Volume:0.0000}";

using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey)))
{
    var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
    var signatureAsString = Convert.ToBase64String(signature);

    // Place signatureAsString into Authorization header of the request
}
```