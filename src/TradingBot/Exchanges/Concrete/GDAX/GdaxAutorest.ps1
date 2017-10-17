#
# GdaxAutorest.ps1
#
autorest --input-file=GdaxSwagger.json --csharp --output-folder=./AutorestClient --namespace=TradingBot.Exchanges.Concrete.GDAX.AutorestClient --add-credentials --sync-methods=none