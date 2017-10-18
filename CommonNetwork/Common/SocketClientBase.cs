using System;
using System.Collections.Generic;
using System.Threading;
using CommonLibs;

namespace CommonNetwork
{
    public class SocketClientBase
    {
        protected Dictionary<int, Action<WebPackage>> m_callbacks = null;
        protected Dictionary<int, WebPackage> m_packages = null;
        protected Dictionary<int, Semaphore> m_semaphores = null;
        protected Dictionary<int, Action<WebPackage>> RegActions = null;

        public Action<bool, string> OnConnect = null;
        public Action<string> OnDisconnect = null;
        public Action<string> OnError = null;

        protected int m_id = 0;
        protected int TimeOutMilliseconds = 5000;
        protected int BufferSize = 1024;

        public SocketClientBase()
        {
            m_id = 0;

            RegActions = new Dictionary<int, Action<WebPackage>>();
            m_callbacks = new Dictionary<int, Action<WebPackage>>();
            m_semaphores = new Dictionary<int, Semaphore>();
            m_packages = new Dictionary<int, WebPackage>();
        }

        public void AddOnConnect(Action<bool, string> callback)
        {
            OnConnect += callback;
        }
        public void AddDisconnect(Action<string> callback)
        {
            OnDisconnect += callback;
        }
        public void AddOnError(Action<string> callback)
        {
            OnError += callback;
        }
        public void RemoveOnConnect(Action<bool, string> callback)
        {
            OnConnect -= callback;
        }
        public void RemoveDisconnect(Action<string> callback)
        {
            OnDisconnect -= callback;
        }
        public void RemoveOnError(Action<string> callback)
        {
            OnError -= callback;
        }
        public void RegActionCallbak(int actionId, Action<WebPackage> callback)
        {
            RegActions[actionId] = callback;
        }

        protected WebPackage CreatePackage(int actionId, byte[] param, int accountId = 0)
        {
            var package = new WebPackage
            {
                ActionId = actionId,
                Uid = accountId,
                ID = System.Threading.Interlocked.Increment(ref m_id),
                Params = param,
            };
            return package;
        }

        public void DoReceivePackage(WebPackage package)
        {
            m_packages[package.ID] = package;
            //根据ActionId注册
            if (package.ErrorCode == ErrorCodeEnum.Success)
            {
                if (RegActions.ContainsKey(package.ActionId))
                    RegActions[package.ActionId](package);
            }
            //根据PacageId注册，每个Package不一样
            if (m_callbacks.ContainsKey(package.ID))
            {
                m_callbacks[package.ID](package);
                m_callbacks.Remove(package.ID);
            }
            if (m_semaphores.ContainsKey(package.ID))
            {
                m_semaphores[package.ID].Release();
                m_semaphores.Remove(package.ID);
            }
        }

    }
}
