using System;

namespace PusherClient.DotNetCore
{
    public class PusherException : Exception
    {
        public ErrorCodes PusherCode { get; set; }

        public PusherException(string message, ErrorCodes code)
            : base(message)
        {
            PusherCode = code;
        }
    }
}