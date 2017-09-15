cd TradingBot/
dotnet publish -c Release -o published/
cd ..
docker-compose build --no-cache
docker-compose up
