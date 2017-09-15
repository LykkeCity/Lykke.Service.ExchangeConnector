using System;
using System.Linq;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Trading;

namespace TradingBot.Repositories
{
    public class TranslatedSignalTableEntity : TableEntity
    {
        public DateTime ReceiveDateTime { get; set; }
        
        public string Exchange { get; set; }
        
        public string Instrument { get; set; }
        
        public int OrderCommandInt { get; set; }

        public OrderCommand OrderCommand
        {
            get => (OrderCommand) OrderCommandInt;
            set => OrderCommandInt = (int) value;
        }
        
        public DateTime SignalDateTime { get; set; }
        
        public string OrderId { get; set; }
        
        public string ExternalId { get; set; }
        
        public double Price { get; set; } // azure table storage don't support decimal
        
        public double Volume { get; set; }
        
        
        public int OrderTypeInt { get; set; }
        public OrderType OrderType
        {
            get => (OrderType) OrderTypeInt;
            set => OrderTypeInt = (int) value;
        }
        
        
        public int TradeTypeInt { get; set; }
        public TradeType TradeType
        {
            get => (TradeType) TradeTypeInt;
            set => TradeTypeInt = (int) value;
        }
        
        
        public int TimeInForceInt { get; set; }
        public TimeInForce TimeInForce
        {
            get => (TimeInForce) TimeInForceInt;
            set => TimeInForceInt = (int) value;
        }
        
        
        public int SignalSourceInt { get; set; }
        public SignalSource SignalSource
        {
            get => (SignalSource) SignalSourceInt;
            set => SignalSourceInt = (int) value;
        }
        
        public string ErrorMessage { get; set; }
        
        
        public int ExecutionStatusInt { get; set; }
        public ExecutionStatus ExecutionStatus
        {
            get => (ExecutionStatus) ExecutionStatusInt;
            set => ExecutionStatusInt = (int) value;
        }
        
        public string ClientIP { get; set; }
        
        public string RequestSentToExchange { get; set; }
        
        public DateTime? RequestToExchangeDateTime { get; set; }
        
        public string ResponseFromExchange { get; set; }
        
        public DateTime? ResponseFromExchangeDateTime { get; set; }
        
        
        public int TranslationStatusInt { get; set; }
        public TranslationStatus Status
        {
            get => (TranslationStatus) TranslationStatusInt;
            set => TranslationStatusInt = (int) value;
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
            Price = (double)signal.Price;
            Volume = (double)signal.Volume;
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
}