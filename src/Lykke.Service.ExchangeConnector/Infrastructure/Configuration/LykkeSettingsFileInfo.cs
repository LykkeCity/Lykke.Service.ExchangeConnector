using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace TradingBot.Infrastructure.Configuration
{
    public class LykkeSettingsFileInfo : IFileInfo
    {
        private readonly Stream fileContentStream;

        public LykkeSettingsFileInfo(Stream fileContentStream)
        {
            this.fileContentStream = fileContentStream;
        }
        
        public Stream CreateReadStream()
        {
            return fileContentStream;
        }

        public bool Exists => true;
        
        public long Length => fileContentStream.Length;
        
        public string PhysicalPath { get; }
        
        public string Name => "appsettings.json";
        
        public DateTimeOffset LastModified { get; }
        
        public bool IsDirectory => false;
    }
}