namespace Lykke.Service.ExchangeDataStore.Core.Services.DataHarvesters
{
    interface IDataHarvester<T>
    {
        void SubscribeToDataStore();
        //event BroadcastData();
    }
}
