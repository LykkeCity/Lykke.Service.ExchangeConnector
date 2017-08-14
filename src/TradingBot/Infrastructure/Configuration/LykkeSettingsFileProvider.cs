using System.IO;
using System.Net.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace TradingBot.Infrastructure.Configuration
{
    public class LykkeSettingsFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string settingsUrl)
        {
            var task = new HttpClient().GetStreamAsync(settingsUrl);
            task.Wait();
            
            return new LykkeSettingsFileInfo(task.Result);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new System.NotImplementedException();
        }

        public IChangeToken Watch(string filter)
        {
            throw new System.NotImplementedException();
        }
    }
}