using System;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{

    public class PackageClientHandler : ChannelHandlerAdapter
    {
        private NettyClient m_parent;

        public PackageClientHandler(NettyClient client)
        {
            m_parent = client;
        }

        /// <summary>
        /// 连接成功
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            m_parent.SetContext(context);
            base.ChannelActive(context);
            if (m_parent.OnConnect != null)
                m_parent.OnConnect(true, "OK");
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            if (m_parent.OnDisconnect != null)
                m_parent.OnDisconnect("");
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (m_parent.OnError != null)
                m_parent.OnError(exception.Message);
            context.CloseAsync();
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var byteBuffer = message as IByteBuffer;
            if (byteBuffer != null)
            {
                try
                {
                    var package = ProtoBufUtils.Deserialize<WebPackage>(byteBuffer.Array);
                    if (package != null)
                    {
                        m_parent.DoReceivePackage(package);
                    }
                }
                catch (Exception e)
                {

                }
            }
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
    }

}
