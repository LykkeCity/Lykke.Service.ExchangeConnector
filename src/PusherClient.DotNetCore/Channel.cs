namespace PusherClient.DotNetCore
{
    public delegate void SubscriptionEventHandler(object sender);
    
    public class Channel : EventEmitter
    {
        private readonly Pusher pusher;

        public event SubscriptionEventHandler Subscribed;
        private readonly string name;

        public bool IsSubscribed { get; private set; }

        public Channel(string channelName, Pusher pusher)
        {
            this.pusher = pusher;
            name = channelName;
        }

        internal virtual void SubscriptionSucceeded(string data)
        {
            if (IsSubscribed)
                return;

            IsSubscribed = true;

            Subscribed?.Invoke(this);
        }

        public void Unsubscribe()
        {
            IsSubscribed = false;
            pusher.Unsubscribe(name);
        }
    }
}