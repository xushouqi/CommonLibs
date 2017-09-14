using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommonLibs;
using System.Net.WebSockets;

namespace CommonNetwork
{
    public class ClientSocket
    {
        private static ClientSocket m_instance = null;
        public static ClientSocket Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ClientSocket();
                }
                return m_instance;
            }
        }

        private ClientWebSocket m_socket;
        private const int BufferSize = 8192;
        private string m_url = "ws://127.0.0.1:22337/ws";
        private Dictionary<int, Action<WebPackage>> m_callbacks = null;
        private Dictionary<int, WebPackage> m_packages = null;
        private Dictionary<int, Semaphore> m_semaphores = null;
        private Task m_waitReceiver;

        public Action<bool, string> OnConnect = null;
        public Action<string> OnDisconnect = null;
        public Action<string> OnError = null;

        public Dictionary<int, Action<WebPackage>> RegActions = null;

        public ClientSocket()
        {
            RegActions = new Dictionary<int, Action<WebPackage>>();
            m_callbacks = new Dictionary<int, Action<WebPackage>>();
            m_semaphores = new Dictionary<int, Semaphore>();
            m_packages = new Dictionary<int, WebPackage>();

            try
            {
                m_socket = new ClientWebSocket();
            }
            catch (Exception ex)
            {
                ClientCommon.DebugLog(ex.Message);
            }
        }

        public bool CheckConnection()
        {
            bool ret = m_socket != null && m_socket.State == WebSocketState.Open;
            return ret;
        }

        public async Task<bool> ConnectServerAsync(string address)
        {
            if (m_socket != null
                 && (m_socket.State == WebSocketState.Connecting || m_socket.State == WebSocketState.Open)
                 )
                return false;

            m_url = "ws://" + address + "/ws";
            string message = "OK";
            bool success = false;
            try
            {
                if (m_waitReceiver == null || m_waitReceiver.Status != TaskStatus.Running)
                {
                    await m_socket.ConnectAsync(new Uri(m_url), new CancellationTokenSource(60000).Token);

                    m_waitReceiver = new Task(() => { WaitReceive().Wait(); });
                    m_waitReceiver.Start();
                    success = true;
                }
                else
                    message = "GodIdentity m_waitReceiver is Still Running!!! Can't Connect!!!";
            }
            catch (Exception ex)
            {
                message = ex.Message;
                success = false;
                ClientCommon.DebugLog(message);
            }
            if (OnConnect != null)
                OnConnect(success, message);
            return success;
        }

        public void ConnectServer(string address, Action<bool, string> onConnect = null)
        {
            if (m_socket != null
                && (m_socket.State == WebSocketState.Connecting || m_socket.State == WebSocketState.Open)
                )
                return;

            m_url = "ws://" + address + "/ws";
            string message = "OK";
            bool success = false;
            try
            {
                if (m_waitReceiver == null || m_waitReceiver.Status != TaskStatus.Running)
                {
                    m_socket.ConnectAsync(new Uri(m_url), new CancellationTokenSource(60000).Token).Wait();

                    m_waitReceiver = new Task(() => { WaitReceive().Wait(); });
                    m_waitReceiver.Start();
                    success = true;
                }
                else
                    message = "GodIdentity m_waitReceiver is Still Running!!! Can't Connect!!!";
            }
            catch (Exception ex)
            {
                message = ex.Message;
                success = false;
                ClientCommon.DebugLog(message);
            }
            if (OnConnect != null)
                OnConnect(success, message);
            if (onConnect != null)
                onConnect(success, message);    
        }

        async Task WaitReceive()
        {
            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);
            var socket = m_socket;

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    //等待数据
                    var incoming = await socket.ReceiveAsync(seg, CancellationToken.None);
                    if (incoming.Count > 0)
                    {
                        if (incoming.MessageType == WebSocketMessageType.Binary)
                        {
                            WebPackage package = ProtoBufUtils.Deserialize<WebPackage>(seg.Array, 0, incoming.Count);
                            //是合法的数据包
                            if (package != null)
                            {
                                m_packages[package.ID] = package;
                                //根据ActionId注册
                                if (package.MyError == ErrorCodeEnum.Success)
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
                        else if (incoming.MessageType == WebSocketMessageType.Text)
                        {

                        }
                        else if (incoming.MessageType == WebSocketMessageType.Close)
                        {
                            OnHandleClose(socket);
                        }
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

                }
                else if (socket.State == WebSocketState.CloseSent)
                {
                }
                else
                {
                    if (OnError != null)
                        OnError(socket.State.ToString());
                }
            }
            catch(Exception e)
            {
                string msg = e.Message;

                OnHandleClose(socket);
            }
        }

        void OnHandleClose(WebSocket socket)
        {
            string massage = "";
            if (socket != null && socket.CloseStatusDescription != null)
                massage = socket.CloseStatusDescription;
            if (OnDisconnect != null)
                OnDisconnect(massage);
        }

        public void CloseConnection()
        {
            if (m_socket != null)
            {
                //m_socket.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).Wait();
                m_socket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationTokenSource(60000).Token).Wait();
            }
        }

        private int m_id = 0;
        private WebPackage CreatePackage(int actionId, byte[] param, int accountId = 0)
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

        public WebPackage Send(int actionId, byte[] param, Action<WebPackage> callback)
        {
            var package = CreatePackage(actionId, param);

            var result = ProtoBufUtils.Serialize(package);
            m_socket.SendAsync(new ArraySegment<byte>(result), WebSocketMessageType.Binary, true, new CancellationTokenSource(60000).Token).Wait();

            m_callbacks[package.ID] = callback;
            return package;
        }

        public async Task<WebPackage> SendAsync(int actionId, byte[] param)
        {
            var package = CreatePackage(actionId, param);

            var result = ProtoBufUtils.Serialize(package);
            await m_socket.SendAsync(new ArraySegment<byte>(result), WebSocketMessageType.Binary, true, new CancellationTokenSource(60000).Token);

            Task task = new Task(() =>
            {
                Semaphore mux = new Semaphore(0, int.MaxValue);
                m_semaphores[package.ID] = mux;
                mux.WaitOne();
            });
            task.Start();
            Task.WaitAll(task);
            m_packages.TryGetValue(package.ID, out package);
            return package;
        }

    }
}