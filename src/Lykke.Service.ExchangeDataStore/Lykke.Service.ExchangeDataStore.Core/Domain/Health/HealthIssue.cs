namespace Lykke.Service.ExchangeDataStore.Core.Domain.Health
{
    public class HealthIssue
    {
        public string Type { get; private set; }
        public string Value { get; private set; }

        public static HealthIssue Create(string type, string value)
        {
            return new HealthIssue
            {
                Type = type,
                Value = value
            };
        }
    }
}