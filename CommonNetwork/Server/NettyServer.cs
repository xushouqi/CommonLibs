using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DotNetty;
using DotNetty.Common;
using DotNetty.Codecs;
using DotNetty.Codecs.Json;
using DotNetty.Codecs.Protobuf;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Handlers;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{
    public class NettyServer 
    {
        private readonly NettyOptions m_config;
        private IChannel m_boundChannel;
        private MultithreadEventLoopGroup m_bossGroup;
        private MultithreadEventLoopGroup m_workerGroup;

        public NettyServer(NettyOptions options)
        {
            m_config = options;
        }

        public async Task CloseServer()
        {
            //关闭服务
            await m_boundChannel.CloseAsync();
            //释放工作组线程
            await Task.WhenAll(
                m_bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                m_workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        }

        public async Task RunServerAsync(IServiceProvider services)
        {
            // 主工作线程组，设置为1个线程
            m_bossGroup = new MultithreadEventLoopGroup(1);
            // 工作线程组，默认为内核数*2的线程数
            m_workerGroup = new MultithreadEventLoopGroup();

            X509Certificate2 tlsCertificate = null;
            if (m_config.IsSsl) //如果使用加密通道
            {
                string curpath = Directory.GetCurrentDirectory();
                tlsCertificate = new X509Certificate2(Path.Combine(curpath, "dotnetty.com.pfx"), "password");
            }

            try
            {
                //声明一个服务端Bootstrap，每个Netty服务端程序，都由ServerBootstrap控制，
                //通过链式的方式组装需要的参数
                var bootstrap = new ServerBootstrap();
                bootstrap
                    // 设置主和工作线程组
                    .Group(m_bossGroup, m_workerGroup)
                    // 设置通道模式为TcpSocket
                    .Channel<TcpServerSocketChannel>()
                    // 设置网络IO参数等
                    .Option(ChannelOption.SoBacklog, 100)
                    //在主线程组上设置一个打印日志的处理器
                    .Handler(new LoggingHandler("SRV-LSTN")) 
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    { 
                        //工作线程连接器 是设置了一个管道，服务端主线程所有接收到的信息都会通过这个管道一层层往下传输
                        //同时所有出栈的消息 也要这个管道的所有处理器进行一步步处理
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null) //Tls的加解密
                        {
                            pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                        }

                        //日志拦截器
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        
                        ////出栈消息，通过这个handler 在消息顶部加上消息的长度
                        //pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));

                        ////入栈消息通过该Handler,解析消息的包长信息，并将正确的消息体发送给下一个处理Handler
                        //pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        
                        //业务handler ，这里是实际处理Echo业务的Handler
                        pipeline.AddLast("echo", new NettySocketHander(services, m_config, this));
                    }));

                // bootstrap绑定到指定端口的行为 就是服务端启动服务，同样的Serverbootstrap可以bind到多个端口
                m_boundChannel = await bootstrap.BindAsync(m_config.Port);
            }
            finally
            {
                //释放工作组线程
                //await Task.WhenAll(
                //    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                //    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

    }
    
}
