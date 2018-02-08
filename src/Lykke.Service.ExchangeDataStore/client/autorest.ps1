# This script uses Autorest to generate service's client library

# == Prerequisites ==

# Nodejs version >= 6.11.2 - https://nodejs.org/en/download/
# NPM version >= 3.10.10 - https://www.npmjs.com/get-npm
# Autorest version >= 1.2.2 - https://www.npmjs.com/package/autorest

# Run this file if you use PowerShell directly
autorest --input-file=http://localhost:5000/swagger/v1/swagger.json --csharp --output-folder=./AutorestClient --namespace=Lykke.Service.ExchangeDataStore.Client.AutorestClient