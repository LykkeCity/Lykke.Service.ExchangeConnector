using System.Linq;

namespace TradingBot.Communications
{
    public static class StringExtension
    {
        public static string GetAzureFriendlyName(this string name)
        {
            return RemoveUnsupportedCharacters(name);
        }
        
        /// <summary>
        /// List of unsupported characters is from 
        /// https://docs.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model
        /// </summary>
        private static string RemoveUnsupportedCharacters(string name)
        {
            return RemoveCharacters(new[] { 
                "/", "\\", "#", "?", "-",
			    
                // Control characters from U+0000 to U+001F, including \t, \n, \r:
                "\u0000", "\u0001", "\u0002", "\u0003", "\u0004", "\u0005", "\u0006", "\u0007", 
                "\u0008", "\u0009", "\u000A", "\u000B", "\u000C", "\u000D", "\u000E", "\u000F", 
                "\u0010", "\u0011", "\u0012", "\u0013", "\u0014", "\u0015", "\u0016", "\u0017", 
                "\u0018", "\u0019", "\u001A", "\u001B", "\u001C", "\u001D", "\u001E", "\u001F", 
			    
                // Control characters from U+007F to U+009F:
                "\u007F", 
                "\u0080", "\u0081", "\u0082", "\u0083", "\u0084", "\u0085", "\u0086", "\u0087", 
                "\u0088", "\u0089", "\u008A", "\u008B", "\u008C", "\u008D", "\u008E", "\u008F", 
                "\u0090", "\u0091", "\u0092", "\u0093", "\u0094", "\u0095", "\u0096", "\u0097", 
                "\u0098", "\u0099", "\u009A", "\u009B", "\u009C", "\u009D", "\u009E", "\u009F",  
            }, name);
        }
	    
        private static string RemoveCharacters(string[] charactersToRemove, string str)
        {
            return charactersToRemove.Aggregate(str, (current, character) => current.Replace(character, string.Empty));
        }
    }
}