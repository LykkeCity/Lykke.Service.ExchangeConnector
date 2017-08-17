FROM microsoft/dotnet:1.1.2-runtime
COPY published/ app/
COPY appsettings.json app/appsettings.json
COPY FIX44.xml app/FIX44.xml
COPY TradingBot.xml app/TradingBot.xml
WORKDIR app/
EXPOSE 5040
ENTRYPOINT ["dotnet", "TradingBot.dll"]
