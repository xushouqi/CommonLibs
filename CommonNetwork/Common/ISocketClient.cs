using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLibs;

namespace CommonNetwork
{
    public interface ISocketClient
    {
        bool CheckConnection();
        Task<bool> ConnectServerAsync(string address, int port, Action<bool, string> onConnect = null);
        bool ConnectServer(string address, int port, Action<bool, string> onConnect = null);
        void CloseConnection();
        WebPackage Send(int actionId, byte[] param, Action<WebPackage> callback);
        Task<WebPackage> SendAsync(int actionId, byte[] param);
        void RegActionCallbak(int actionId, Action<WebPackage> callback);
        void AddOnConnect(Action<bool, string> callback);
        void AddDisconnect(Action<string> callback);
        void AddOnError(Action<string> callback);
        void RemoveOnConnect(Action<bool, string> callback);
        void RemoveDisconnect(Action<string> callback);
        void RemoveOnError(Action<string> callback);
        void Dispatch();
    }
}
