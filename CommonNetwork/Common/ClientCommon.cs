using System;
using System.Collections.Concurrent;

namespace CommonNetwork
{
    public class ClientCommon
    {
        private static ConcurrentDictionary<string, string> UrlDb = new ConcurrentDictionary<string, string>();

        public static void InitialUrl(string serverName, string apiUrl)
        {
            UrlDb.AddOrUpdate(serverName, apiUrl, (key, oldValue)=> apiUrl);
        }
        public static string GetUrl(string serverName)
        {
            string url = "";
            UrlDb.TryGetValue(serverName, out url);
            return url;
        }

        public static void DebugLog(string words, params object[] args)
        {
            Console.WriteLine(words, args);
        }
    }
}
