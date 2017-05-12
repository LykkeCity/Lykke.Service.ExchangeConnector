FROM microsoft/dotnet:latest
WORKDIR /app
COPY src/app /app
ENTRYPOINT dotnet TradingBot.dll
