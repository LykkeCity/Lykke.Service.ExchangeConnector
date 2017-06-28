cd TradingBot/
dotnet publish -c Release -o published/
cd ../TradingBot.TheAlphaEngine/
dotnet publish -c Release -o published/
cd ..
docker-compose build --no-cache
docker-compose up
