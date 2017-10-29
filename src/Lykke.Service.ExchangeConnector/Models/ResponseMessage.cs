namespace TradingBot.Models
{
    public class ResponseMessage
    {
        public ResponseMessage()
        {    
        }
        
        public ResponseMessage(string message)
        {
            Message = message;
        }

        public ResponseMessage(string message, object model)
        {
            Message = message;
            Model = model;
        }

        public string Message { get; set; }

        public object Model { get; set; }
    }
}