namespace PusherClient.DotNetCore
{
    public class Settings
    {
        public static readonly Settings Default = new Settings()
        {
            ClientName = "PusherClient.DotNetCore",
            VersionNumber = "0.0.1",
            ProtocolVersion = "5"
        }; 
        
        public string ProtocolVersion { get; private set; }
        
        public string ClientName { get; private set; }
        
        public string VersionNumber { get; private set; }
    }
}