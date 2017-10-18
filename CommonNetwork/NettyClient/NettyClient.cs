using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{
    public class NettyClient : SocketClientBase, ISocketClient
    {
        private readonly NettyOptions m_config;
        private IChannel m_clientChannel;
        private IChannelHandlerContext m_clientContext;
        private MultithreadEventLoopGroup m_group;

        public NettyClient(NettyOptions options)
        {
            m_config = options;
        }
        public void SetContext(IChannelHandlerContext context)
        {
            m_clientContext = context;
        }
        public bool CheckConnection()
        {
            return m_clientChannel != null && m_clientChannel.Active;
        }

        public void CloseConnection()
        {
            if (CheckConnection())
            {
                try
                {
                    m_group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)).Wait();
                    m_group = null;

                    m_clientChannel.CloseAsync().Wait();
                    m_clientChannel = null;
                }
                catch (Exception e)
                {
                    OnError(e.Message);
                }
            }
        }
        
        public async Task<bool> ConnectServerAsync(string address, int port, Action<bool, string> onConnect = null)
        {
            bool ret = false;
            string message = "";
            m_group = new MultithreadEventLoopGroup();

            X509Certificate2 cert = null;
            string targetHost = null;
            if (m_config.IsSsl)
            {
                string curpath = Directory.GetCurrentDirectory();
                cert = new X509Certificate2(Path.Combine(curpath, "dotnetty.com.pfx"), "password");
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(m_group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoKeepalive, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (cert != null)
                        {
                            pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }
                        pipeline.AddLast(new LoggingHandler());
                        //pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        //pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        // 用于检查链接的状态，比如写超时，读超时, 发送心跳包
                        pipeline.AddLast("timeout", new IdleStateHandler(0, 0, m_config.HeartbeatInterval / 1000));

                        pipeline.AddLast("echo", new PackageClientHandler(this));
                    }));

                m_clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(address), port));
                ret = m_clientChannel != null;
            }
            finally
            {
                //await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
            if (OnConnect != null)
                OnConnect(ret, message);
            if (onConnect != null)
                onConnect(ret, message);
            return ret;
        }

        public WebPackage Send(int actionId, byte[] param, Action<WebPackage> callback)
        {
            var package = CreatePackage(actionId, param);

            var msg = Unpooled.Buffer(m_config.BufferSize);
            byte[] bytes = ProtoBufUtils.Serialize(package);
            msg.WriteBytes(bytes);
            m_clientChannel.WriteAndFlushAsync(msg).Wait();

            m_callbacks[package.ID] = callback;
            return package;
        }

        public async Task<WebPackage> SendAsync(int actionId, byte[] param)
        {
            WebPackage package = null;
            if (CheckConnection())
            {
                package = CreatePackage(actionId, param);

                var msg = Unpooled.Buffer(m_config.BufferSize);
                byte[] bytes = ProtoBufUtils.Serialize(package);
                msg.WriteBytes(bytes);
                await m_clientChannel.WriteAndFlushAsync(msg);

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
