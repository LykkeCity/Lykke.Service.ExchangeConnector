# TradingBot

Implementation of The Alpha Engine: Automated Trading Algorithm. 
See algorithm's description in the paper: https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2951348

This software uses demo API of OANDA API: http://developer.oanda.com/rest-live-v20/introduction/

## How to run the software:

The TradingBot uses demo access to OANDA API. For using it you should open an account on https://oanda.com and create access token on the Manage API Access page here: https://www.oanda.com/demo-account/tpa/personal_token

After that is done you need to put the token into `src/OandaApi/OandaAuth.cs` file.

### To run the software:

1. Execute `dotnet restore`
2. Execute `dotnet run`

Demo mode will be started: It will obtain an `accountId` for provided token, then it will print account details including balance.
Finally, it will open price stream for `EUR_USD` instrument and starts to print prices and The Alpha Engine intrinsic time events: `Overshoot` or `Directional change`. See [the paper](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2951348) for details.

After two minutes the demo is over and program will automatically stop.

## How to run it in Docker:

1. Clone the repo to /TradingBot directory
2. Go to that directory and execute `dotnet restore` command to restore dependencies
3. Execute `dotnet build -c release` command to build project
4. Execute `dotnet publish -c release -o app` command to create `app` folder with all project's files
5. Execute `docker build --tag tradingbot` command to build docker image named `tradingbot`
6. Execute `docker run tradingbot` for run container from the image

