
using System.Net;

namespace CommonLibs
{
    public static class HttpExtensions
    {
        public static bool IsSuccessStatusCode(this HttpWebResponse response)
        {
            return ((int)response.StatusCode >= 200) && ((int)response.StatusCode <= 299);
        }
    }
}
