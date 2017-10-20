using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommonLibs;

namespace CommonNetwork
{
    public class TcpSocketClient : SocketClientBase, ISocketClient
    {
        private TcpClient m_tcpClient;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoDispatch">是否自动分发，Unity中选择False</param>
        public TcpSocketClient(bool autoDispatch) : base(autoDispatch)
        {

        }

        public bool CheckConnection()
        {
            return m_tcpClient != null && m_tcpClient.Connected;
        }

        public void CloseConnection()
        {
            try
            {
                if (m_tcpClient != null && m_tcpClient.Connected)
                    m_tcpClient.Close();
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
            if (OnDisconnect != null)
                OnDisconnect("");
        }

        CancellationTokenSource m_receiveCancelSource;

        public bool ConnectServer(string address, int port, Action<bool, string> onConnect = null)
        {
            bool ret = false;
            string message = "OK";
            m_tcpClient = new TcpClient();
            try
            {
                if (m_receiveCancelSource == null)
                {
                    m_tcpClient.Connect(address, port);
                    m_tcpClient.NoDelay = true;
                    m_tcpClient.ReceiveBufferSize = BufferSize;
                    m_tcpClient.SendBufferSize = BufferSize;
                    ret = true;

                    m_receiveCancelSource = new CancellationTokenSource();
                    //等待接收数据
                    Task task = new Task(() => WaitToReceive().Wait(m_receiveCancelSource.Token));
                    task.Start();
                }
                else
                    message = "WaitReceive is Still Running!!! Can't Connect!!!";
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            if (OnConnect != null)
                OnConnect(ret, message);
            if (onConnect != null)
                onConnect(ret, message);
            return ret;
        }

        public async Task<bool> ConnectServerAsync(string address, int port, Action<bool, string> onConnect = null)
        {
            bool ret = false;
            string message = "OK";
            m_tcpClient = new TcpClient();
            try
            {
                if (m_receiveCancelSource == null)
                {
                    await m_tcpClient.ConnectAsync(address, port);
                    m_tcpClient.NoDelay = true;
                    m_tcpClient.ReceiveBufferSize = BufferSize;
                    m_tcpClient.SendBufferSize = BufferSize;
                    ret = true;

                    m_receiveCancelSource = new CancellationTokenSource();
                    //等待接收数据
                    Task task = new Task(() => WaitToReceive().Wait(m_receiveCancelSource.Token));
                    task.Start();
                }
                else
                    message = "WaitReceive is Still Running!!! Can't Connect!!!";
            }
            catch(Exception e)
            {
                message = e.Message;
            }
            if (OnConnect != null)
                OnConnect(ret, message);
            if (onConnect != null)
                onConnect(ret, message);
            return ret;
        }

        async Task WaitToReceive()
        {
            byte[] receiveBytes = new byte[m_tcpClient.ReceiveBufferSize];
            int numberOfBytesRead = 0;
            NetworkStream ns = m_tcpClient.GetStream();

            while (m_tcpClient.Connected && ns.CanRead)
            {
                try
                {
                    numberOfBytesRead = await ns.ReadAsync(receiveBytes, 0, m_tcpClient.ReceiveBufferSize);
                    WebPackage package = ProtoBufUtils.Deserialize<WebPackage>(receiveBytes, 0, numberOfBytesRead);
                    //是合法的数据包
                    if (package != null)
                    {
                        DoReceivePackage(package);
                    }
                }
                catch (SocketException e)
                {
                    if (ns != null)
                        ns.Close();
                    if (m_tcpClient != null && m_tcpClient.Connected)
                        m_tcpClient.Close();

                    if (OnError != null)
                        OnError(e.Message);
                    break;
                }
                catch (Exception e)
                {

                }
            }
            if (OnDisconnect != null)
                OnDisconnect("");
        }

        public WebPackage Send(int actionId, byte[] param, Action<WebPackage> callback)
        {
            WebPackage package = null;
            if (CheckConnection())
            {
                package = m_packageManager.CreateRequestPackage(actionId, 0, 0, param);
                var result = ProtoBufUtils.Serialize(package);

                NetworkStream ns = m_tcpClient.GetStream();
                if (ns.CanWrite)
                {
                    ns.Write(result, 0, result.Length);

                    m_callbacks[package.ID] = callback;
                }
            }
            return package;
        }

        public async Task<WebPackage> SendAsync(int actionId, byte[] param)
        {
            WebPackage package = null;
            if (CheckConnection())
            {
                NetworkStream ns = m_tcpClient.GetStream();
                if (ns.CanWrite)
                {
                    package = m_packageManager.CreateRequestPackage(actionId, 0, 0, param);
                    var result = ProtoBufUtils.Serialize(package);

                    ns.Write(result, 0, result.Length);
                    //todo: QUESTION!!! Why sometimes not send????
                    //await ns.WriteAsync(result, 0, result.Length);

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
            }
            else
                package = m_packageManager.CreatePackage(PackageTypeEnum.Act, actionId, 0, 0, ErrorCodeEnum.Disconnected);
            return await Task.FromResult(package);
        }
    }
}
