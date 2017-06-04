FROM microsoft/dotnet:latest
WORKDIR /app
COPY src/publishedapp /app
ENTRYPOINT dotnet TradingBot.dll
