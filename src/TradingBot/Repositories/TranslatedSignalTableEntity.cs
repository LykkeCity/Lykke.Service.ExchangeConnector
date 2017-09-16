using System;
using System.Linq;
using System.Net.Http;
using TradingBot.Trading;

namespace TradingBot.Repositories
{
    public enum SignalSource
    {
        RabbitQueue,
        RestApi
    }

    public enum TranslationStatus
    {
        Success,
        Failure
    }

    public class TranslatedSignalTableEntity : BaseEntity
    {
        public DateTime ReceiveDateTime { get; set; }
        
        public string Exchange { get; set; }
        
        public string Instrument { get; set; }

        public OrderCommand OrderCommand { get; set; }
        
        public DateTime SignalDateTime { get; set; }
        
        public string OrderId { get; set; }
        
        public string ExternalId { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal Volume { get; set; }

        public OrderType OrderType { get; set; }
        
        public TradeType TradeType { get; set; }

        public TimeInForce TimeInForce { get; set; }
        
        public SignalSource SignalSource { get; set; }

        public string ErrorMessage { get; set; }
        
        public ExecutionStatus ExecutionStatus { get; set; }
        
        public string ClientIP { get; set; }
        
        public string RequestSentToExchange { get; set; }
        
        public DateTime? RequestToExchangeDateTime { get; set; }
        
        public string ResponseFromExchange { get; set; }
        
        public DateTime? ResponseFromExchangeDateTime { get; set; }
        
        public TranslationStatus Status { get; set; }
        
        public static string GeneratePartitionKey()
        {
            return "TranslatedSignal";
        }
        
        public TranslatedSignalTableEntity()
        {
        }

        public TranslatedSignalTableEntity(SignalSource signalSource, string exchange, string instrument, TradingSignal signal)
        {
            ReceiveDateTime = DateTime.UtcNow;
            Status = TranslationStatus.Success;

            SignalSource = signalSource;
            Exchange = exchange;
            Instrument = instrument;

            SignalDateTime = signal.Time;
            OrderCommand = signal.Command;
            OrderId = signal.OrderId;
            Price = signal.Price;
            Volume = signal.Volume;
            OrderType = signal.OrderType;
            TradeType = signal.TradeType;
            TimeInForce = signal.TimeInForce;
        }

        public void SetKeys(string partitionKey, long rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey.ToString();
        }

        public void SetExecutionResult(ExecutedTrade executedTrade)
        {
            ExecutionStatus = executedTrade.Status;
            ErrorMessage = executedTrade.Message;
        }

        public void RequestSent(string content)
        {
            RequestToExchangeDateTime = DateTime.UtcNow;
            RequestSentToExchange = content;
        }

        public void RequestSent(HttpMethod httpMethod, string url, HttpContent httpContent)
        {
            RequestToExchangeDateTime = DateTime.UtcNow;
            RequestSentToExchange =
                $"{httpMethod} {url} HEADERS: {string.Join("; ", httpContent.Headers.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}"))} " +
                $"BODY: {httpContent.ReadAsStringAsync().Result}";
        }

        public void ResponseReceived(string content)
        {
            ResponseFromExchange = content;
            ResponseFromExchangeDateTime = DateTime.UtcNow;
        }

        public void Failure(Exception e)
        {
            var error = "";
            
            do
            {
                error += $"{e.Message}\n{e.StackTrace}\n";
            } while ((e = e.InnerException) != null);
            
            Failure(error);
        }
        
        public void Failure(string error)
        {
            Status = TranslationStatus.Failure;
            ErrorMessage = error;
        }
    }
}