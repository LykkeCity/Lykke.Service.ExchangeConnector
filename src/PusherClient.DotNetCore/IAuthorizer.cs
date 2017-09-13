namespace PusherClient.DotNetCore
{
    public interface IAuthorizer
    {
        string Authorize(string channelName, string socketId);
    }
}