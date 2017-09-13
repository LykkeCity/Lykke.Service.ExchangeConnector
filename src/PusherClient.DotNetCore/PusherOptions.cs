namespace PusherClient.DotNetCore
{
    public class PusherOptions
    {
        public bool Encrypted = false;
        public IAuthorizer Authorizer = null;
        public string Cluster = "mt1";

        internal string Host => string.Format("ws-{0}.pusher.com", this.Cluster);
    }
}