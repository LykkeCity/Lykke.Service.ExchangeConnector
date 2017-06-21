FROM microsoft/dotnet:latest
COPY published/ app/
COPY appsettings.json app/appsettings.json
WORKDIR app/
EXPOSE 5040
ENTRYPOINT ["dotnet", "TradingBot.dll"]
