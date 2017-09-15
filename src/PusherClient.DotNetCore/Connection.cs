using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using Common.Log;

namespace PusherClient.DotNetCore
{
    public class Connection
    {
        private int backOffMillis;

        private static readonly int MaxBackoffMillis = 10000;
        private static readonly int BackOffMillisIncrement = 1000;
        
        private readonly Pusher pusher;
        private readonly string url;
        private readonly ILog _log;

        private WebSocket websocket;
        public ConnectionState State { get; private set; } = ConnectionState.Initialized;
        private bool allowReconnect;
        public string SocketId { get; private set; }

        public event ErrorEventHandler Error;
        public event ConnectedEventHandler Connected;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;

        public Connection(Pusher pusher, string url, ILog log)
        {
            this.pusher = pusher;
            this.url = url;
            _log = log;
        }

        internal void Connect()
        {
            ChangeState(ConnectionState.Connecting);
            allowReconnect = true;

            websocket = new WebSocket(url);
            websocket.EnableAutoSendPing = true;
            websocket.AutoSendPingInterval = 1;
            websocket.Opened += websocket_Opened;
            websocket.Error += websocket_Error;
            websocket.Closed += websocket_Closed;
            websocket.MessageReceived += websocket_MessageReceived;
            websocket.Open();
        }
        
        private void ChangeState(ConnectionState state)
        {
            State = state;
            ConnectionStateChanged?.Invoke(this, State);
        }
        
        private void websocket_Opened(object sender, EventArgs e)
        {
            _log.WriteInfoAsync(
                nameof(PusherClient),
                nameof(Connection),
                nameof(ChangeState),
                "Websocket opened OK.")
                .Wait();
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            _log.WriteWarningAsync(
                nameof(PusherClient),
                nameof(Connection),
                nameof(ChangeState),
                "Websocket connection has been closed")
                .Wait();

            ChangeState(ConnectionState.Disconnected);
            websocket = null;

            if (allowReconnect)
            {
                ChangeState(ConnectionState.WaitingToReconnect);
                Task.Delay(backOffMillis).Wait();
                backOffMillis = Math.Min(MaxBackoffMillis, backOffMillis + BackOffMillisIncrement);
                Connect();
            }
        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _log.WriteErrorAsync(
                nameof(PusherClient),
                nameof(Connection),
                nameof(ChangeState),
                e.Exception)
                .Wait();

            // TODO: What happens here? Do I need to re-connect, or do I just log the issue?
        }
        
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _log.WriteInfoAsync(
                nameof(PusherClient),
                nameof(Connection),
                nameof(ChangeState),
                "Websocket message received: " + e.Message)
                .Wait();

            Debug.WriteLine(e.Message);

            // DeserializeAnonymousType will throw and error when an error comes back from pusher
            // It stems from the fact that the data object is a string normally except when an error is sent back
            // then it's an object.

            // bad:  "{\"event\":\"pusher:error\",\"data\":{\"code\":4201,\"message\":\"Pong reply not received\"}}"
            // good: "{\"event\":\"pusher:error\",\"data\":\"{\\\"code\\\":4201,\\\"message\\\":\\\"Pong reply not received\\\"}\"}";

            var jObject = JObject.Parse(e.Message);

            if (jObject["data"] != null && jObject["data"].Type != JTokenType.String)
                jObject["data"] = jObject["data"].ToString(Formatting.None);

            string jsonMessage = jObject.ToString(Formatting.None);
            var template = new { @event = String.Empty, data = String.Empty, channel = String.Empty };

            //var message = JsonConvert.DeserializeAnonymousType(e.Message, template);
            var message = JsonConvert.DeserializeAnonymousType(jsonMessage, template);

            pusher.EmitEvent(message.@event, message.data);

            if (message.@event.StartsWith("pusher"))
            {
                // Assume Pusher event
                switch (message.@event)
                {
                    case Constants.Error:
                        ParseError(message.data);
                        break;

                    case Constants.ConnectionEstablished:
                        ParseConnectionEstablished(message.data);
                        break;

                    case Constants.ChannelSubscriptionSucceeded:

                        if (pusher.Channels.ContainsKey(message.channel))
                        {
                            var channel = pusher.Channels[message.channel];
                            channel.SubscriptionSucceeded(message.data);
                        }

                        break;

                    case Constants.ChannelSubscriptionError:

                        RaiseError(new PusherException("Error received on channel subscriptions: " + e.Message, ErrorCodes.SubscriptionError));
                        break;

                    default:
                        throw new NotSupportedException($"{message.@event} is not supported");
                        
//                    case Constants.ChannelMemberAdded:
//
//                        // Assume channel event
//                        if (pusher.Channels.ContainsKey(message.channel))
//                        {
//                            var channel = _pusher.Channels[message.channel];
//
//                            if (channel is PresenceChannel)
//                            {
//                                ((PresenceChannel)channel).AddMember(message.data);
//                                break;
//                            }
//                        }
//
//                        //Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
//                        break;
//
//                    case Constants.ChannelMemberRemoved:
//
//                        // Assume channel event
//                        if (_pusher.Channels.ContainsKey(message.channel))
//                        {
//                            var channel = _pusher.Channels[message.channel];
//
//                            if (channel is PresenceChannel)
//                            {
//                                ((PresenceChannel)channel).RemoveMember(message.data);
//                                break;
//                            }
//                        }
//
//                        Pusher.Trace.TraceEvent(TraceEventType.Warning, 0, "Received a presence event on channel '" + message.channel + "', however there is no presence channel which matches.");
//                        break;
                }
            }
            else
            {
                // Assume channel event
                if (pusher.Channels.ContainsKey(message.channel))
                    pusher.Channels[message.channel].EmitEvent(message.@event, message.data);
            }
        }
        
        private void ParseConnectionEstablished(string data)
        {
            var template = new { socket_id = string.Empty };
            var message = JsonConvert.DeserializeAnonymousType(data, template);
            SocketId = message.socket_id;

            ChangeState(ConnectionState.Connected);

            Connected?.Invoke(this);
        }

        private void ParseError(string data)
        {
            var template = new { message = string.Empty, code = (int?) null };
            var parsed = JsonConvert.DeserializeAnonymousType(data, template);

            ErrorCodes error = ErrorCodes.Unkown;

            if (parsed.code != null && Enum.IsDefined(typeof(ErrorCodes), parsed.code))
            {
                error = (ErrorCodes)parsed.code;
            }

            RaiseError(new PusherException(parsed.message, error));
        }
        
        private void RaiseError(PusherException error)
        {
            // if a handler is registerd, use it, otherwise throw
            var handler = Error;
            if (handler != null) handler(this, error);
            else throw error;
        }
        
        internal void Send(string message)
        {
            if (State == ConnectionState.Connected)
            {
                _log.WriteInfoAsync(
                    nameof(PusherClient),
                    nameof(Connection),
                    nameof(ChangeState),
                    "Sending: " + message)
                    .Wait();
                Debug.WriteLine("Sending: " + message);
                websocket.Send(message);
            }
        }
        
        internal void Disconnect()
        {
            allowReconnect = false;

            websocket.Opened -= websocket_Opened;
            websocket.Error -= websocket_Error;
            websocket.Closed -= websocket_Closed;
            websocket.MessageReceived -= websocket_MessageReceived;
            websocket.Close();

            ChangeState(ConnectionState.Disconnected);
        }
    }
}