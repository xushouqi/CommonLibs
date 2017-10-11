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
    public class UserApi
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
        /// 创建指定类型账号（仅限超级管理员）
        /// </summary>
        public static async Task<ReturnData<AccountData>> CreateAccountByTypeAsync(string token, string username, string password, CommonLibs.UserTypeEnum atype)
        {
            string qstr = "username=" + username.ToString()+"&" + "password=" + password.ToString()+"&" + "atype=" + ((int)atype).ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/CreateAccountByType";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 注册，只需用户名密码
        /// </summary>
        public static async Task<ReturnData<AccountData>> RegisterAsync(string username, string password)
        {
            string qstr = "username=" + username.ToString()+"&" + "password=" + password.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/Register";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 修改密码
        /// </summary>
        public static async Task<ReturnData<bool>> ChangePasswordAsync(string username, string oldpassword, string password)
        {
            string qstr = "username=" + username.ToString()+"&" + "oldpassword=" + oldpassword.ToString()+"&" + "password=" + password.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/ChangePassword";
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
		/// <summary>
        /// 修改密码（管理员）
        /// </summary>
        public static async Task<ReturnData<bool>> ChangePasswordByAdminAsync(string token, string username, string password)
        {
            string qstr = "username=" + username.ToString()+"&" + "password=" + password.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/ChangePasswordByAdmin";
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
		/// <summary>
        /// 验证用户名密码（默认加密）
        /// </summary>
        public static async Task<ReturnData<AccountData>> LoginAsync(string username, string password)
        {
            string qstr = "username=" + username.ToString()+"&" + "password=" + password.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/Login";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 刷新token
        /// </summary>
        public static async Task<ReturnData<AccountData>> UpdateMyTokenAsync(string token)
        {
            string qstr = "";
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/UpdateMyToken";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 验证TOKEN并返回ACCOUNT
        /// </summary>
        public static async Task<ReturnData<AccountData>> ValidAccountByTokenAsync(string token)
        {
            string qstr = "token=" + token.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/ValidAccountByToken";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 登出
        /// </summary>
        public static async Task<ReturnData<bool>> LogoutAsync(string token)
        {
            string qstr = "";
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/Logout";
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
		/// <summary>
        /// 输入身份证（正确后MyState改为Approval）
        /// </summary>
        public static async Task<ReturnData<bool>> ModifyIDCodeAsync(string token, string name, string idcode)
        {
            string qstr = "name=" + name.ToString()+"&" + "idcode=" + idcode.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/ModifyIDCode";
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
		/// <summary>
        /// 修改手机号（仅限服务端）
        /// </summary>
        public static async Task<ReturnData<bool>> ModifyMobileAsync(string token, int accountid, string mobile)
        {
            string qstr = "accountid=" + accountid.ToString()+"&" + "mobile=" + mobile.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/ModifyMobile";
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
		/// <summary>
        /// 获取手机号（仅限服务端）
        /// </summary>
        public static async Task<ReturnData<string>> GetMobileAsync(string username)
        {
            string qstr = "username=" + username.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/GetMobile";
            ReturnData<string> retData = new ReturnData<string>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<string>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 验证身份证是否重复
        /// </summary>
        public static async Task<ReturnData<bool>> CheckValidIDCodeAsync(string idcode)
        {
            string qstr = "idcode=" + idcode.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/CheckValidIDCode";
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
		/// <summary>
        /// 根据用户名获取账号信息
        /// </summary>
        public static async Task<ReturnData<AccountData>> GetAccountByUsernameAsync(string username)
        {
            string qstr = "username=" + username.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/GetAccountByUsername";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 根据ID获取账号信息
        /// </summary>
        public static async Task<ReturnData<AccountData>> GetAccountByIdAsync(string token, int id)
        {
            string qstr = "id=" + id.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/GetAccountById";
            ReturnData<AccountData> retData = new ReturnData<AccountData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<AccountData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 分配一个可用游服
        /// </summary>
        public static async Task<ReturnData<ServerData>> GetCurrentServerAsync(string token)
        {
            string qstr = "";
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/GetCurrentServer";
            ReturnData<ServerData> retData = new ReturnData<ServerData>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<ServerData>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 生成一个TOTP的一次性付款码
        /// </summary>
        public static async Task<ReturnData<string>> GetTOTPCodeAsync(string token)
        {
            string qstr = "";
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/GetTOTPCode";
            ReturnData<string> retData = new ReturnData<string>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<string>>(tmp);
                }
            }
            return retData;
        }
		/// <summary>
        /// 识别一次性付款码
        /// </summary>
        public static async Task<ReturnData<string>> CheckTOTPCodeAsync(string code)
        {
            string qstr = "code=" + code.ToString();
            byte[] datas = RsaService.EncryptFromString(qstr, "");
            string url = ClientCommon.GetUrl("GodIdentity") + "/User/CheckTOTPCode";
            ReturnData<string> retData = new ReturnData<string>();
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
					retData = JsonConvert.DeserializeObject<ReturnData<string>>(tmp);
                }
            }
            return retData;
        }

    }
}
