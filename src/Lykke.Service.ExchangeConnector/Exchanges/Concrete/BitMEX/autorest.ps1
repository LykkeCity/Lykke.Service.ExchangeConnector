#
# autorest.ps1
#
autorest --input-file=BitMex.json --csharp --output-folder=./AutorestClient --namespace=TradingBot.Exchanges.Concrete.AutorestClient --add-credentials --sync-methods=none