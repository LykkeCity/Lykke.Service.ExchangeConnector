using System;
using System.Collections.Generic;
using Common.Log;
using Newtonsoft.Json;

namespace PusherClient.DotNetCore
{
    public delegate void ErrorEventHandler(object sender, PusherException error);
    public delegate void ConnectedEventHandler(object sender);
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionState state);
    
    public class Pusher : EventEmitter
    {
        private readonly string applicationKey;
        private readonly PusherOptions options;
        private readonly ILog _log;
        private Connection connection;
        public event ConnectedEventHandler Connected;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();
        private ErrorEventHandler errorEvent;
        
        private readonly object lockingObject = new object();
        
        public ConnectionState State => connection?.State ?? ConnectionState.Disconnected;

        public Pusher(string applicationKey, ILog log, PusherOptions options = null)
        {
            this.applicationKey = applicationKey;
            _log = log;
            this.options = options ?? new PusherOptions();
        }

        public void Connect()
        {
            lock (lockingObject)
            {
                // Ensure we only ever attempt to connect once
                if (connection != null)
                {
                    _log.WriteWarningAsync(
                        nameof(PusherClient),
                        nameof(Pusher),
                        nameof(Connect),
                        "Attempt to connect when another connection has already started. New attempt has been ignored.")
                        .Wait();
                    return;
                }
                var scheme = "ws://";

                if (options.Encrypted)
                    scheme = "wss://";

                string url = String.Format("{0}{1}/app/{2}?protocol={3}&client={4}&version={5}",
                    scheme, options.Host, applicationKey, Settings.Default.ProtocolVersion, Settings.Default.ClientName,
                    Settings.Default.VersionNumber);

                connection = new Connection(this, url, _log);
                RegisterEventsOnConnection();
                connection.Connect();
            }
        }
        
        private void RegisterEventsOnConnection()
        {
            connection.Connected += connection_Connected;
            connection.ConnectionStateChanged += connection_ConnectionStateChanged;
            
            if (errorEvent != null)
            {
                // subscribe to the connection's error handler
                foreach (var @delegate in errorEvent.GetInvocationList())
                {
                    if (@delegate is ErrorEventHandler handler)
                        connection.Error += handler;
                }
            }
        }

        private void connection_Connected(object sender)
        {
            Connected?.Invoke(sender);
        }
        
        private void connection_ConnectionStateChanged(object sender, ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Disconnected:
                    MarkChannelsAsUnsubscribed();
                    break;
                case ConnectionState.Connected:
                    SubscribeExistingChannels();
                    break;
            }

            ConnectionStateChanged?.Invoke(sender, state);
        }
        
        private void MarkChannelsAsUnsubscribed()
        {
            foreach (var channel in Channels)
            {
                channel.Value.Unsubscribe();
            }

        }

        private void SubscribeExistingChannels()
        {
            foreach (var channel in Channels)
            {
                Subscribe(channel.Key);
            }
        }
        
        public Channel Subscribe(string channelName)
        {
            if (AlreadySubscribed(channelName))
            {
                _log.WriteWarningAsync(
                    nameof(PusherClient),
                    nameof(Pusher),
                    nameof(Connect),
                    "Channel '" + channelName + "' is already subscribed to. Subscription event has been ignored.")
                    .Wait();
                return Channels[channelName];
            }

            // If private or presence channel, check that auth endpoint has been set
            var chanType = ChannelTypes.Public;

            if (channelName.ToLower().StartsWith("private-"))
                chanType = ChannelTypes.Private;
            else if (channelName.ToLower().StartsWith("presence-"))
                chanType = ChannelTypes.Presence;

            return SubscribeToChannel(chanType, channelName);
        }
        
        private bool AlreadySubscribed(string channelName)
        {
            return Channels.ContainsKey(channelName) && Channels[channelName].IsSubscribed;
        }

        private Channel SubscribeToChannel(ChannelTypes type, string channelName)
        {
            if (!Channels.ContainsKey(channelName))
                CreateChannel(type, channelName);

            if (State == ConnectionState.Connected)
            {
                if (type == ChannelTypes.Presence || type == ChannelTypes.Private)
                {
                    string jsonAuth = options.Authorizer.Authorize(channelName, connection.SocketId);

                    var template = new { auth = String.Empty, channel_data = String.Empty };
                    var message = JsonConvert.DeserializeAnonymousType(jsonAuth, template);

                    connection.Send(JsonConvert.SerializeObject(new { @event = Constants.ChannelSubscribe, data = new { channel = channelName, auth = message.auth, channel_data = message.channel_data } }));
                }
                else
                {
                    // No need for auth details. Just send subscribe event
                    connection.Send(JsonConvert.SerializeObject(new { @event = Constants.ChannelSubscribe, data = new { channel = channelName } }));
                }
            }

            return Channels[channelName];
        }
        
        private void CreateChannel(ChannelTypes type, string channelName)
        {
            switch (type)
            {
                case ChannelTypes.Public:
                    Channels.Add(channelName, new Channel(channelName, this));
                    break;
                    
//                case ChannelTypes.Private:
//                    AuthEndpointCheck();
//                    Channels.Add(channelName, new PrivateChannel(channelName, this));
//                    break;
//                case ChannelTypes.Presence:
//                    AuthEndpointCheck();
//                    Channels.Add(channelName, new PresenceChannel(channelName, this));
//                    break;
                
                default:
                    throw new NotSupportedException($"{type} is not supported");
            }
        }
        
        private void AuthEndpointCheck()
        {
            if (options.Authorizer == null)
            {
                var pusherException = new PusherException("You must set a ChannelAuthorizer property to use private or presence channels", ErrorCodes.ChannelAuthorizerNotSet);
                RaiseError(pusherException);
                throw pusherException;
            }
        }
        
        private void RaiseError(PusherException error)
        {
            errorEvent?.Invoke(this, error);
        }
        
        internal void Unsubscribe(string channelName)
        {
            if (connection.State == ConnectionState.Connected)
                connection.Send(JsonConvert.SerializeObject(new { @event = Constants.ChannelUnsubscribe, data = new { channel = channelName } }));
        }
        
        public event ErrorEventHandler Error
        {
            add
            {
                errorEvent += value;
                if (connection != null)
                {
                    connection.Error += value;
                }
            }
            remove
            {
                errorEvent -= value;
                if (connection != null)
                {
                    connection.Error -= value;
                }
            }
        }
        
        public void Disconnect()
        {
            UnregisterEventsOnDisconnection();
            MarkChannelsAsUnsubscribed();
            connection.Disconnect();
            connection = null;
        }
        
        private void UnregisterEventsOnDisconnection()
        {
            if (connection != null)
            {
                connection.Connected -= connection_Connected;
                connection.ConnectionStateChanged -= connection_ConnectionStateChanged;

                if (errorEvent != null)
                {
                    // unsubscribe to the connection's error handler
                    foreach (var @delegate in errorEvent.GetInvocationList())
                    {
                        if (@delegate is ErrorEventHandler handler)
                            connection.Error -= handler;
                    }
                }
            }
        }
    }
}