using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
    public class NettyClient
    {
        private readonly NettyOptions m_config;

        public NettyClient(NettyOptions options)
        {
            m_config = options;
        }

        public async Task RunClientAsync()
        {
            var group = new MultithreadEventLoopGroup();

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
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (cert != null)
                        {
                            pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }
                        pipeline.AddLast(new LoggingHandler());
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                        // 用于检查链接的状态，比如写超时，读超时, 发送心跳包
                        pipeline.AddLast("timeout", new IdleStateHandler(0, 0, m_config.HeartbeatInterval / 1000));

                        pipeline.AddLast("echo", new EchoClientHandler());
                    }));

                IChannel clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(m_config.Host), m_config.Port));

                Console.ReadLine();

                await clientChannel.CloseAsync();
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        public class EchoClientHandler : ChannelHandlerAdapter
        {
            readonly IByteBuffer initialMessage;

            public EchoClientHandler()
            {
                this.initialMessage = Unpooled.Buffer(1024);
                byte[] messageBytes = Encoding.UTF8.GetBytes("Hello world");
                this.initialMessage.WriteBytes(messageBytes);
            }

            /// <summary>
            /// 重写基类方法，当链接上服务器后，马上发送Hello World消息到服务端
            /// </summary>
            /// <param name="context"></param>
            public override void ChannelActive(IChannelHandlerContext context)
            {
                var data = new MailData();
                data.Key = new Random().Next(100, 200);
                data.Content = string.Concat("content_", new Random().Next(1, 100));
                var tmp = ProtoBufUtils.Serialize(data);

                var meg = Unpooled.Buffer(1024);
                meg.WriteBytes(tmp);

                context.WriteAndFlushAsync(meg);
                //context.WriteAndFlushAsync(data);
            }
            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);
            }

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var byteBuffer = message as IByteBuffer;
                if (byteBuffer != null)
                {
                    Console.WriteLine("Received from server: " + byteBuffer.ToString(Encoding.UTF8));
                }
                //context.WriteAsync(message);
            }

            public override void ChannelReadComplete(IChannelHandlerContext context)
            {
                context.Flush();
            }

            //ChannelHandlerAdapter 重写UserEventTriggered
            public override void UserEventTriggered(IChannelHandlerContext context, object evt)
            {
                if (evt is IdleStateEvent)
                {
                    var eventState = evt as IdleStateEvent;
                    if (eventState != null)
                    {
                        //DatagramPacket builder = new DatagramPacket();
                        //builder.Sender(Packet.PacketType.HEARTBEAT);
                        //Packet packet = builder.build();
                        //ctx.writeAndFlush(packet);

                        //this..SendHeartbeatAsync(context, eventState);
                    }
                }
            }

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                Console.WriteLine("Exception: " + exception);
                context.CloseAsync();
            }
        }

    }
}
