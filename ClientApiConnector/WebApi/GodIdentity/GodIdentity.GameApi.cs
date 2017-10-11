using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using CommonLibs;
using CommonNetwork;
using GodModels.ViewModels;
using Newtonsoft.Json;

namespace ClientApi.GodIdentity
{
    public class GameApi
    {
		/*
        private static HttpClient _client = new HttpClient();
        private static HttpClient client
        {
            get
            {
                if (_client == null)
                    _client = new HttpClient();
                return _client;
            }
        }
		*/

				/// <summary>
        /// GS请求连接
        /// </summary>
        public static async Task<ReturnData<bool>> TryConnectIdentityAsync(string GSID, int userCount)
        {
            string qstr = "GSID=" + GSID.ToString()+"&" + "userCount=" + userCount.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "GS");
            string url = ClientCommon.GetUrl("GodIdentity") + "/Game/TryConnectIdentity";
            ReturnData<bool> retData = new ReturnData<bool>();
            retData.ErrorCode = ErrorCodeEnum.ResponseError;

            var response = await HttpWebResponseUtility.CreatePostHttpResponse(url, datas, null);
            if (response.IsSuccessStatusCode())
            {
				int length = 0;
                byte[] buffer = new byte[1000];
                using (var responseStream = response.GetResponseStream())
                {
                    length = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                }
                if (length > 0)
                {
                    byte[] result = new byte[length];
                    Array.Copy(buffer, result, length);
                    var tmp = Encoding.UTF8.GetString(result);
					retData = JsonConvert.DeserializeObject<ReturnData<bool>>(tmp);
                }
            }
            return retData;
        }

    }
}
