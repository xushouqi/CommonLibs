using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ProtoBuf;
using CommonLibs;

namespace CommonNetwork
{
    public interface ISocketHandler
    {

    }

    public class WebSocketHandler<T> where T : UserData
    {
        private readonly RequestDelegate m_next;
        private readonly ILogger<WebSocketHandler<T>> m_logger;
        private readonly IUserManager<T> m_userManager;
        private readonly IPushManager m_pushManager;
        private IServiceProvider m_services;
        private const int BufferSize = 8192;
        private Assembly m_assembly;
        private WebSocket m_socket = null;
        private UserSocketManager m_userSocketManager;

        private readonly string m_project_name = string.Empty;

        public WebSocketHandler(RequestDelegate next,
            IUserManager<T> userManager,
            IPushManager pushManager,
            UserSocketManager contextManager,
            ILogger<WebSocketHandler<T>> logService
            )
        {
            m_assembly = Assembly.GetEntryAssembly();
            m_project_name = m_assembly.FullName.Split(',')[0];

            m_next = next;
            m_userManager = userManager;
            m_pushManager = pushManager;
            m_logger = logService;
            m_userSocketManager = contextManager;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                m_socket = socket;
                m_services = context.RequestServices;

                m_userSocketManager.Set(socket);
                string channel = socket.GetHashCode().ToString();
                m_logger.LogInformation("SocketHandler Work Start, {0}", Thread.CurrentThread.ManagedThreadId);

                var buffer = new byte[BufferSize];
                var seg = new ArraySegment<byte>(buffer);

                try
                {
                    while (socket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult incoming = null;
                        //等待客户端提交数据
                        //incoming = await socket.ReceiveAsync(seg, CancellationToken.None);
                        do
                        {
                            incoming = await socket.ReceiveAsync(seg, CancellationToken.None).ConfigureAwait(false);
                        }
                        while (!incoming.EndOfMessage);

                        if (incoming.MessageType == WebSocketMessageType.Binary)
                        {
                            WebPackage package = ProtoBufUtils.Deserialize<WebPackage>(seg.Array, 0, incoming.Count);
                            //是合法的数据包
                            if (package != null)
                            {
                                await DoReceive(package, channel);
                            }
                        }
                        else if (incoming.MessageType == WebSocketMessageType.Text)
                        {
                            string msg = System.Text.Encoding.UTF8.GetString(seg.Array, 0, incoming.Count);
                            m_logger.LogError("Receive Text: {0}", msg);
                        }
                        else if (incoming.MessageType == WebSocketMessageType.Close)
                        {
                            OnHandleClose(socket);
                        }
                    }

                    if (socket.State == WebSocketState.Aborted)
                    {
                        // Handle aborted
                    }
                    else if (socket.State == WebSocketState.Closed)
                    {
                        OnHandleClose(socket);
                    }
                    else if (socket.State == WebSocketState.CloseReceived)
                    {
                        OnHandleClose(socket);
                    }
                    else if (socket.State == WebSocketState.CloseSent)
                    {
                    }
                }
                catch (Exception e)
                {
                    string msg = e.Message;
                    m_logger.LogError("Socket Error: {0}", msg);

                    OnHandleClose(socket);
                }
                m_logger.LogInformation("SocketHandler Work Finish. {0}", Thread.CurrentThread.ManagedThreadId);
            }
            else
                await m_next.Invoke(context);
        }

        private async Task DoReceive(WebPackage package, string channel)
        {
            //调用对应的服务
            var actionName = string.Concat(m_project_name, ".Actions.Action", package.ActionId);
            m_logger.LogInformation("DoReceive actionName={0}, Thread={1}", actionName, Thread.CurrentThread.ManagedThreadId);

            try
            {
                Type atype = m_assembly.GetType(actionName);
                if (atype != null)
                {
                    var action = (IAction)m_services.GetService(atype);
                    if (action != null)
                    {
                        int uid = 0;
                        var user = m_userManager.GetUserData(channel);
                        if (user != null)
                        {
                            //注册下发数据
                            m_pushManager.AddPushAction(user, PushToClient);

                            if (package.Uid <= 0 || package.Uid == user.ID)
                                uid = user.ID;
                        }

                        bool validGo = true;
                        AuthPolicyAttribute attri = null;
                        //需验证用户身份权限
                        if (atype.GetTypeInfo().IsDefined(typeof(AuthPolicyAttribute), false))
                        {
                            attri = (AuthPolicyAttribute)atype.GetTypeInfo().GetCustomAttribute(typeof(AuthPolicyAttribute), false);
                            if (attri.AuthPolicy > UserTypeEnum.None)
                                validGo = user != null && user.Type >= attri.AuthPolicy;
                        }

                        if (uid > 0 || (attri != null && attri.AuthPolicy == UserTypeEnum.None))
                        {
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                            WebPackage result = null;
                            if (validGo)
                            {
                                //提交参数
                                if (user != null)
                                    action.Submit(channel, user, package);
                                else
                                    action.Submit(channel, UserConnTypeEnum.Tcp, uid, package);

                                //执行
                                sw.Start();
                                await action.DoAction();
                                sw.Stop();

                                //获取返回值
                                result = action.GetReturnPackage();
                            }
                            else
                                result = action.GetUnAuthorizedPackage(package);

                            //回包
                            await SendAsync(channel, package);

                            m_logger.LogError("NettyAct: {0}, thread={1}, DoActionTime: {2}ms",
                                actionName, Thread.CurrentThread.ManagedThreadId, sw.Elapsed.TotalMilliseconds);

                            //其他后续操作
                            await action.AfterAction();
                        }
                        else
                            m_logger.LogError("Uid NOT Found!!! {0}", actionName);
                    }
                    else
                        m_logger.LogError("{0} NOT Found!!!", actionName);
                }
                else
                    m_logger.LogError("{0} NOT Found!!!", actionName);
            }
            catch (Exception e)
            {
                m_logger.LogError("DoReceive.Exception: {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        private async void PushToClient(WebPackage package)
        {
            var userData = m_userManager.GetUserDataById(package.Uid);
            if (userData != null)
            {
                await Task.Run(() => SendAsync(userData.Channel, package));
                m_logger.LogInformation("PushToClient.Send {0}", Thread.CurrentThread.ManagedThreadId);
            }
        }
        private async Task SendAsync(string channel, WebPackage package)
        {
            await m_userSocketManager.SendPackageToUser(package);
        }

        void OnHandleClose(WebSocket socket)
        {
            if (socket != null)
            {
                var userData = m_userManager.GetUserData(socket.GetHashCode().ToString());
                if (userData != null)
                {
                    //移除下发消息
                    m_pushManager.RemovePushAction(userData);
                }
                //移除user
                int id = m_userManager.RemoveUser(socket.GetHashCode().ToString());

                m_userSocketManager.Remove(socket);

                m_logger.LogInformation("OnHandleClose: {0}, uid={1}", socket.State, id);
                socket.Abort();
            }
        }
    }

    //public class SocketPushData
    //{
    //    public async Task Send(WebSocket socket, ArraySegment<byte> data)
    //    {
    //        await socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
    //    }
    //}
}