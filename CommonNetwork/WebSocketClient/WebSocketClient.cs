using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommonLibs;
using System.Net.WebSockets;

namespace CommonNetwork
{
    public class WebSocketClient : SocketClientBase, ISocketClient
    {
        private ClientWebSocket m_socket;
        private string m_url = "ws://127.0.0.1:22337/ws";

        private Task m_waitReceiver;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoDispatch">是否自动分发，Unity中选择False</param>
        public WebSocketClient(bool autoDispatch) : base(autoDispatch)
        {
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

        CancellationTokenSource m_receiveCancelSource;

        public bool ConnectServer(string address, int port, Action<bool, string> onConnect = null)
        {
            if (m_socket != null
                 && (m_socket.State == WebSocketState.Connecting || m_socket.State == WebSocketState.Open)
                 )
                return false;

            m_url = string.Concat("ws://", address, ":", port, "/ws");
            string message = "OK";
            bool success = false;
            try
            {
                if (m_receiveCancelSource == null)
                {
                    m_socket.ConnectAsync(new Uri(m_url), new CancellationTokenSource(TimeOutMilliseconds).Token).Wait();

                    m_receiveCancelSource = new CancellationTokenSource();
                    Task task = new Task(() => { WaitReceive().Wait(m_receiveCancelSource.Token); });
                    task.Start();
                    success = true;
                }
                else
                    message = "WaitReceive is Still Running!!! Can't Connect!!!";
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
            return success;
        }

        public async Task<bool> ConnectServerAsync(string address, int port, Action<bool, string> onConnect = null)
        {
            if (m_socket != null
                 && (m_socket.State == WebSocketState.Connecting || m_socket.State == WebSocketState.Open)
                 )
                return false;

            m_url = string.Concat("ws://", address, ":", port, "/ws");
            string message = "OK";
            bool success = false;
            try
            {
                if (m_receiveCancelSource == null)
                {
                    await m_socket.ConnectAsync(new Uri(m_url), new CancellationTokenSource(TimeOutMilliseconds).Token);

                    m_receiveCancelSource = new CancellationTokenSource();
                    Task task = new Task(() => { WaitReceive().Wait(m_receiveCancelSource.Token); });
                    task.Start();
                    success = true;
                }
                else
                    message = "WaitReceive is Still Running!!! Can't Connect!!!";
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
            return success;
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
                                DoReceivePackage(package);
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
            if (m_socket != null && (m_socket.State == WebSocketState.Open || m_socket.State == WebSocketState.Connecting))
            {
                try
                {
                    //m_socket.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).Wait();
                    m_socket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationTokenSource(TimeOutMilliseconds).Token);

                    if (m_receiveCancelSource != null && !m_receiveCancelSource.IsCancellationRequested)
                    {
                        m_receiveCancelSource.Cancel();
                        m_receiveCancelSource = null;
                    }
                }
                catch (Exception e)
                {
                    OnError(e.Message);
                }
            }
        }

        public WebPackage Send(int actionId, byte[] param, Action<WebPackage> callback)
        {
            var package = m_packageManager.CreateRequestPackage(actionId, 0, 0, param);
            var result = ProtoBufUtils.Serialize(package);
            m_socket.SendAsync(new ArraySegment<byte>(result), WebSocketMessageType.Binary, true, new CancellationTokenSource(TimeOutMilliseconds).Token);

            m_callbacks[package.ID] = callback;
            return package;
        }

        public async Task<WebPackage> SendAsync(int actionId, byte[] param)
        {
            WebPackage package = null;
            if (CheckConnection())
            {
                package = m_packageManager.CreateRequestPackage(actionId, 0, 0, param);
                var result = ProtoBufUtils.Serialize(package);
                await m_socket.SendAsync(new ArraySegment<byte>(result), WebSocketMessageType.Binary, true, new CancellationTokenSource(TimeOutMilliseconds).Token);

                Task<bool> task = new Task<bool>(() =>
                {
                    Semaphore mux = new Semaphore(0, int.MaxValue);
                    m_semaphores[package.ID] = mux;
                    bool ret = mux.WaitOne(TimeOutMilliseconds);
                    return ret;
                });
                task.Start();
                Task.WaitAll(task);
                if (task.Result)
                    m_packages.TryGetValue(package.ID, out package);
                else
                    package.ErrorCode = ErrorCodeEnum.TimeOut;
            }
            else
                package = m_packageManager.CreatePackage(PackageTypeEnum.Act, actionId, 0, 0, ErrorCodeEnum.Disconnected);
            return await Task.FromResult(package);
        }

    }
}