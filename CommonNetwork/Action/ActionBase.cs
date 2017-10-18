using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public class ActionBase<T> : BaseDisposable, IAction
    {
        protected UserConnTypeEnum ConnType = UserConnTypeEnum.WebApi;
        protected string m_channel;
        protected UserData m_userData = null;
        protected int m_actionId = 0;
        protected ReturnData<T> m_return;
        protected WebPackage m_package;
        protected PackageParams m_params;
        protected int m_accountId = 0;
        protected WebSocket m_socket = null;

        protected override void Dispose(bool disposing)
        {
            if (m_params != null)
                m_params.Dispose();
            base.Dispose(disposing);
        }
        public void Submit(string channel, UserConnTypeEnum connType, int accountId, WebPackage package)
        {
            ConnType = connType;
            m_channel = channel;
            m_accountId = accountId;
            m_package = package;
            if (m_package.Params != null)
                m_params = new PackageParams(m_package.Params);
        }
        public void Submit(string channel, UserData userData, WebPackage package)
        {
            ConnType = userData.ConnType;
            m_channel = channel;
            m_userData = userData;
            m_accountId = m_userData != null ? m_userData.ID : 0;
            m_package = package;
            if (m_package.Params != null)
                m_params = new PackageParams(m_package.Params);
        }
        public virtual async Task DoAction()
        {
        }
        public WebPackage GetReturnPackage()
        {
            if (m_return != null)
            {
                m_package.ErrorCode = m_return.ErrorCode;
                if (m_return.Data != null)
                    m_package.Return = ProtoBufUtils.Serialize(m_return.Data);
            }
            else
                m_package.ErrorCode = ErrorCodeEnum.Unknown;
            return m_package;
        }
        //public virtual byte[] GetResponseData()
        //{
        //    GetReturnPackage();
        //    var result = ProtoBufUtils.Serialize(m_package);
        //    return result;
        //}
        public virtual WebPackage GetUnAuthorizedPackage(WebPackage package)
        {
            m_package = package;
            package.ErrorCode = ErrorCodeEnum.UnAuthorized;
            return m_package;
        }
        public virtual async Task AfterAction()
        {
        }
    }
}
