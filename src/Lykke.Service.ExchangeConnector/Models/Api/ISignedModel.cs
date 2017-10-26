namespace TradingBot.Models.Api
{
    public interface ISignedModel
    {
        string GetStringToSign();
    }
}