namespace PusherClient.DotNetCore
{
    public class PrivateChannel : Channel
    {
        public PrivateChannel(string channelName, Pusher pusher) : base(channelName, pusher) { }
    }
}