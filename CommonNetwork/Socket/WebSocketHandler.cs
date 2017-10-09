using System;
using System.Collections.Generic;
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
    public class WebSocketHandler<T> where T : UserData
    {
        private readonly RequestDelegate m_next;
        private readonly ILogger<WebSocketHandler<T>> m_logService;
        private readonly IUserManager<T> m_userManager;
        private readonly IPushManager m_pushManager;
        private IServiceProvider m_services;
        private const int BufferSize = 8192;
        private Assembly m_assembly;
        private WebSocket m_socket = null;

        private readonly string m_project_name = string.Empty;

        public WebSocketHandler(RequestDelegate next,
            IUserManager<T> userManager,
            IPushManager pushManager,
            ILogger<WebSocketHandler<T>> logService
            )
        {
            m_assembly = Assembly.GetEntryAssembly();
            m_project_name = m_assembly.FullName.Split(',')[0];

            m_next = next;
            m_userManager = userManager;
            m_pushManager = pushManager;
            m_logService = logService;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                m_socket = socket;
                m_services = context.RequestServices;
                
                m_logService.LogInformation("SocketHandler Work Start, {0}", Thread.CurrentThread.ManagedThreadId);

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
                                //调用对应的服务
                                var actionName = string.Concat(m_project_name, ".Actions.Action", package.ActionId);
                                m_logService.LogInformation("SocketHandler ReceiveAsync actionName={0}, Thread={1}", actionName, Thread.CurrentThread.ManagedThreadId);

                                try
                                {
                                    Type atype = m_assembly.GetType(actionName);
                                    if (atype != null)
                                    {
                                        var action = (IAction)m_services.GetService(atype);
                                        if (action != null)
                                        {
                                            int uid = 0;
                                            var user = m_userManager.GetUserData(socket);
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
                                                byte[] result;
                                                if (validGo)
                                                {
                                                    //提交参数
                                                    action.Submit(socket, user, package);

                                                    //执行
                                                    await action.DoAction();

                                                    //获取返回值
                                                    result = action.GetResponseData();
                                                }
                                                else
                                                    result = action.GetUnAuthorizedData(package);

                                                var ia = new ArraySegment<byte>(result);
                                                //回包
                                                await SendAsync(socket, ia);
                                                //await socket.SendAsync(ia, WebSocketMessageType.Binary, true, CancellationToken.None);

                                                //其他后续操作
                                                await action.AfterAction();
                                            }
                                            else
                                                m_logService.LogError("Uid NOT Found!!! {0}", actionName);
                                        }
                                        else
                                            m_logService.LogError("{0} NOT Found!!!", actionName);
                                    }
                                    else
                                        m_logService.LogError("{0} NOT Found!!!", actionName);
                                }
                                catch (WebSocketException e)
                                {
                                    m_logService.LogError("SocketHandle.WebSocketException: {0}", e.Message);
                                }
                                catch (Exception e)
                                {
                                    m_logService.LogError("SocketHandle.Exception: {0}\n{1}", e.Message, e.StackTrace);
                                }
                            }
                        }
                        else if (incoming.MessageType == WebSocketMessageType.Text)
                        {
                            string msg = System.Text.Encoding.UTF8.GetString(seg.Array, 0, incoming.Count);
                            m_logService.LogError("Receive Text: {0}", msg);
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
                    m_logService.LogError("Socket Error: {0}", msg);

                    OnHandleClose(socket);
                }
                m_logService.LogInformation("SocketHandler Work Finish. {0}", Thread.CurrentThread.ManagedThreadId);
            }
            else
                await m_next.Invoke(context);
        }
        
        private async void PushToClient(WebPackage package)
        {
            var result = ProtoBufUtils.Serialize(package);
            var ia = new ArraySegment<byte>(result);

            m_logService.LogInformation("PushToClient.Send {0}, data={1}", Thread.CurrentThread.ManagedThreadId, ia.Count);
            await Task.Run(() => SendAsync(m_socket, ia));
        }
        private async Task SendAsync(WebSocket socket, ArraySegment<byte> data)
        {
            await socket.SendAsync(data, WebSocketMessageType.Binary, true, new CancellationTokenSource(60000).Token);
        }

        void OnHandleClose(WebSocket socket)
        {
            if (socket != null)
            {
                var userData = m_userManager.GetUserData(socket);
                if (userData != null)
                {
                    //移除下发消息
                    m_pushManager.RemovePushAction(userData);
                }
                //移除user
                int id = m_userManager.RemoveUser(socket);
                m_logService.LogInformation("OnHandleClose: {0}, uid={1}", socket.State, id);
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