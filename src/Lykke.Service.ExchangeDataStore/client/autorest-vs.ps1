# This script uses Autorest to generate service's client library

# == Prerequisites ==

# Nodejs version >= 6.11.2 - https://nodejs.org/en/download/
# NPM version >= 3.10.10 - https://www.npmjs.com/get-npm
# Autorest version >= 1.2.2 - https://www.npmjs.com/package/autorest

# Run this file if your use Execute as Script command of Visual Studio's PowerShell Tools extension
autorest --input-file=http://localhost:5000/swagger/v1/swagger.json --csharp --output-folder=./client/Lykke.Service.ExchangeDataStore.Client/AutorestClient --namespace=Lykke.Service.ExchangeDataStore.Client.AutorestClient