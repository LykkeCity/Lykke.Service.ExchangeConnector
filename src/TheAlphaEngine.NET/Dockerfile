FROM microsoft/dotnet:latest
COPY published/ app/
COPY appsettings.json app/appsettings.json
WORKDIR app/
ENTRYPOINT ["dotnet", "TheAlphaEngine.NET.dll"]