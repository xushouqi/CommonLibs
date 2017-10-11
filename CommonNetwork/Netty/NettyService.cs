using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using DotNetty;
using DotNetty.Common;
using DotNetty.Codecs;
using DotNetty.Codecs.Json;
using DotNetty.Codecs.Protobuf;
using DotNetty.Buffers;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{
    [ProtoContract]
    public class MailData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        /// <summary>
        ///  收件人TeamID
        /// </summary>
        [ProtoMember(2)]
        public int ToTeamId { get; set; }
        /// <summary>
        /// 发送者的ID
        /// </summary>
        [ProtoMember(3)]
        public int SenderId { get; set; }
        /// <summary>
        /// 发送者类型：玩家Role或选手Player
        /// </summary>
        [ProtoMember(4)]
        public int SenderType { get; set; }
        [ProtoMember(5)]
        public string Title { get; set; }
        [ProtoMember(6)]
        public string Content { get; set; }
        [ProtoMember(7)]
        public int Type { get; set; }
        [ProtoMember(8)]
        public int State { get; set; }
        /// <summary>
        /// 邮件待处理的目标ID，与邮件Type有关，如PlayerId, CupId
        /// </summary>
        [ProtoMember(9)]
        public int TargetId { get; set; }
        /// <summary>
        /// 附件奖励ID
        /// </summary>
        [ProtoMember(10)]
        public int RewardId { get; set; }

    }

    public class NettyService
    {
        private readonly NettyOptions m_config;
        private IChannel m_boundChannel;
        private NettyContextManager m_contextManager;

        public NettyService(NettyOptions options)
        {
            m_config = options;
        }

        public async Task RunServerAsync(NettyContextManager manager)
        {
            m_contextManager = manager;

            // 主工作线程组，设置为1个线程
            var bossGroup = new MultithreadEventLoopGroup(1);
            // 工作线程组，默认为内核数*2的线程数
            var workerGroup = new MultithreadEventLoopGroup();

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
                    .Group(bossGroup, workerGroup)
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
                        
                        //出栈消息，通过这个handler 在消息顶部加上消息的长度
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));

                        //入栈消息通过该Handler,解析消息的包长信息，并将正确的消息体发送给下一个处理Handler
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        
                        //业务handler ，这里是实际处理Echo业务的Handler
                        pipeline.AddLast("echo", new EchoServerHandler(m_contextManager));
                    }));

                // bootstrap绑定到指定端口的行为 就是服务端启动服务，同样的Serverbootstrap可以bind到多个端口
                m_boundChannel = await bootstrap.BindAsync(m_config.Port);

                Console.ReadLine();
                //关闭服务
                await m_boundChannel.CloseAsync();
            }
            finally
            {
                //释放工作组线程
                await Task.WhenAll(
                    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        /// <summary>
        /// 管道处理基类，较常用
        /// </summary>
        public class EchoServerHandler : ChannelHandlerAdapter
        {
            private readonly NettyContextManager m_contextManager;

            public EchoServerHandler(NettyContextManager manager)
            {
                m_contextManager = manager;
            }

            public override void ChannelActive(IChannelHandlerContext context)
            {
                m_contextManager.Set(context);
                base.ChannelActive(context);
            }
            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);
                m_contextManager.Remove(context);
            }

            /// <summary>
            /// 重写基类的方法，当消息到达时触发，这里收到消息后，在控制台输出收到的内容，并原样返回了客户端
            /// </summary>
            /// <param name="context"></param>
            /// <param name="message"></param>
            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var buffer = message as IByteBuffer;
                if (buffer != null)
                {
                    var tmp = ProtoBufUtils.Deserialize<MailData>(buffer.ToArray());

                    Console.WriteLine("Received from client: " + tmp.Content);
                    //context.WriteAsync(buffer);//写入输出流

                    Task.Delay(1000).Wait();
                    m_contextManager.Test();
                }
            }

            /// <summary>
            /// 输出到客户端，也可以在上面的方法中直接调用WriteAndFlushAsync方法直接输出
            /// </summary>
            /// <param name="context"></param>
            public override void ChannelReadComplete(IChannelHandlerContext context)
            {
                context.Flush();
            }

            /// <summary>
            /// 捕获 异常，并输出到控制台后断开链接，提示：客户端意外断开链接，也会触发
            /// </summary>
            /// <param name="context"></param>
            /// <param name="exception"></param>
            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                Console.WriteLine("Exception: " + exception);
                context.CloseAsync();
            }
        }
    }
}
