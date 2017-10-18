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

        public bool CheckConnection()
        {
            return m_tcpClient != null && m_tcpClient.Connected;
        }

        public void CloseConnection()
        {
            if (m_tcpClient != null && m_tcpClient.Connected)
                m_tcpClient.Close();

            if (OnDisconnect != null)
                OnDisconnect("");
        }
        
        public async Task<bool> ConnectServerAsync(string address, int port, Action<bool, string> onConnect = null)
        {
            bool ret = false;
            string message = "OK";
            m_tcpClient = new TcpClient();
            try
            {
                await m_tcpClient.ConnectAsync(address, port);
                m_tcpClient.NoDelay = true;
                m_tcpClient.ReceiveBufferSize = BufferSize;
                m_tcpClient.SendBufferSize = BufferSize;
                ret = true;

                //等待接收数据
                Task task = new Task(() => WaitToReceive().Wait());
                task.Start();
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
                package = CreatePackage(actionId, param);
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
                package = CreatePackage(actionId, param);
                var result = ProtoBufUtils.Serialize(package);

                NetworkStream ns = m_tcpClient.GetStream();
                if (ns.CanWrite)
                {
                    await ns.WriteAsync(result, 0, result.Length);
                }

                Task task = new Task(() =>
                {
                    Semaphore mux = new Semaphore(0, int.MaxValue);
                    m_semaphores[package.ID] = mux;
                    mux.WaitOne();
                });
                task.Start();
                if (Task.WaitAll(new Task[] { task }, TimeOutMilliseconds))
                    m_packages.TryGetValue(package.ID, out package);
                else
                    package.ErrorCode = ErrorCodeEnum.TimeOut;
            }
            return package;
        }
    }
}
