using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CommonLibs
{
    /// <summary>
    /// 有关HTTP请求的辅助类
    /// </summary>
    public class HttpUtility
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        /// <summary>
        /// 创建GET方式的HTTP请求
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="timeout">请求的超时时间</param>
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>
        /// <returns></returns>
        public static async Task<HttpWebResponse> CreateGetHttpResponse(string url, int? timeout, string userAgent, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");

            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = DefaultUserAgent;
            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(userAgent))
                request.UserAgent = userAgent;

            if (timeout.HasValue)
                request.Timeout = timeout.Value;

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            return await request.GetResponseAsync() as HttpWebResponse;
        }

        public static async Task<string> GetHttpContent(string url, Encoding encoding, int? timeout, string userAgent, CookieCollection cookies)
        {
            string result = string.Empty;
            var res = await CreateGetHttpResponse(url, timeout, userAgent, cookies);
            try
            {
                using (var sr = new StreamReader(res.GetResponseStream(), encoding))
                {
                    result = sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = reader.ReadToEnd();
                            Console.WriteLine("GetHttp {0} Error: {1}", url, error);
                        }
                    }
                }
            }
            return result;
        }

        public async static Task<CookieCollection> LoginXueQiu(string url, string username, string password, CookieCollection cookies)
        {
            CookieCollection cookie = null;
            bool ret = false;
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("username", username);
            paras.Add("password", password);
            paras.Add("remember_me", "true");
            try
            {
                var response = await CreatePostHttpResponse(url, paras, null, "", Encoding.UTF8, null);
                ret = response.IsSuccessStatusCode();
                var cookieStr = response.Headers.Get("Set-Cookie");
                if (cookieStr.Contains("remember"))
                {
                    CookieContainer con = new CookieContainer();
                    var uri = new Uri("https://xueqiu.com");
                    con.SetCookies(uri, cookieStr);
                    cookie = con.GetCookies(uri);
                }
                response.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return cookie;
        }

        /// <summary>
        /// 创建POST方式的HTTP请求
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="parameters">随同请求POST的参数名称及参数值字典</param>
        /// <param name="timeout">请求的超时时间</param>
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>
        /// <param name="requestEncoding">发送HTTP请求时所用的编码</param>
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>
        /// <returns></returns>
        public static async Task<HttpWebResponse> CreatePostHttpResponse(string url, IDictionary<string, string> parameters, int? timeout, string userAgent, Encoding requestEncoding, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");
            if (requestEncoding == null)
                throw new ArgumentNullException("requestEncoding");

            HttpWebRequest request = null;
            //如果是发送HTTPS请求
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.CreateHttp(url);
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
                request = WebRequest.CreateHttp(url);

            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers = headers;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(userAgent))
                request.UserAgent = userAgent;
            else
                request.UserAgent = DefaultUserAgent;

            if (timeout.HasValue)
                request.Timeout = timeout.Value;

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            //如果需要POST数据
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    else
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                    i++;
                }
                byte[] datas = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(datas, 0, datas.Length);
                }
            }
            return await request.GetResponseAsync() as HttpWebResponse;
        }
        public static async Task<HttpWebResponse> CreatePostHttpResponse(string url, string inputs, CookieCollection cookies = null)
        {
            byte[] datas = RsaService.EncryptFromString(inputs);
            return await CreatePostHttpResponse(url, datas, cookies);
        }

        public static async Task<HttpWebResponse> CreatePostHttpResponse(string url, byte[] datas, CookieCollection cookies = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            HttpWebRequest request = null;
            //如果是发送HTTPS请求
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "POST";
            request.ContentType = "application/octet-stream";
            request.Timeout = 30000;

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            if (datas != null)
            {
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(datas, 0, datas.Length);
                }
            }
            return await request.GetResponseAsync() as HttpWebResponse;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受
        }
    }

    public class BigFormUrlEncodedContent : ByteArrayContent
    {
        public BigFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
            : base(BigFormUrlEncodedContent.GetContentByteArray(nameValueCollection))
        {
            base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }
        private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            if (nameValueCollection == null)
            {
                throw new ArgumentNullException("nameValueCollection");
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> current in nameValueCollection)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append('&');
                }

                stringBuilder.Append(BigFormUrlEncodedContent.Encode(current.Key));
                stringBuilder.Append('=');
                stringBuilder.Append(BigFormUrlEncodedContent.Encode(current.Value));
            }
            return Encoding.Default.GetBytes(stringBuilder.ToString());
        }
        private static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }
            return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
        }
    }
}