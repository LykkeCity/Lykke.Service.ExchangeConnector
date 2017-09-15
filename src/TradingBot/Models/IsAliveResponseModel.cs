namespace TradingBot.Models
{
    /// <summary>
    /// Checks service is alive response
    /// </summary>
    public class IsAliveResponseModel
    {
        /// <summary>API version</summary>
        public string Version { get; set; }

        /// <summary>Environment variables</summary>
        public string Env { get; set; }
    }
}
