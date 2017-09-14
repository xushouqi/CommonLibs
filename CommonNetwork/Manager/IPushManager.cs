using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public interface IPushManager
    {
        void AddPushAction(UserData userData, Action<WebPackage> callback);
        void RemovePushAction(UserData userData);
        void PushDataById<T>(int id, T data);
    }
}
