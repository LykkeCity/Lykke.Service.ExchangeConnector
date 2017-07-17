FROM microsoft/dotnet:latest
COPY published/ app/
COPY appsettings.json app/appsettings.json
COPY FIX44.xml app/FIX44.xml
WORKDIR app/
EXPOSE 5040
ENTRYPOINT ["dotnet", "TradingBot.dll"]
