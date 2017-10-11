using System;
using System.Collections.Concurrent;
using System.Text;
using DotNetty.Transport.Channels;
using DotNetty.Buffers;

namespace CommonNetwork
{
    public class NettyContextManager
    {
        private ConcurrentDictionary<string, IChannelHandlerContext> m_contexts;

        public NettyContextManager()
        {
            m_contexts = new ConcurrentDictionary<string, IChannelHandlerContext>();
        }

        public void Set(IChannelHandlerContext context)
        {
            m_contexts.AddOrUpdate(context.Channel.Id.ToString(), context, (key, oldValue) => context);
        }
        public void Remove(IChannelHandlerContext context)
        {
            m_contexts.TryRemove(context.Channel.Id.ToString(), out context);
        }

        public void Test()
        {
            foreach (var item in m_contexts)
            {
                var initialMessage = Unpooled.Buffer(1024);
                byte[] messageBytes = Encoding.UTF8.GetBytes("Hello world");
                initialMessage.WriteBytes(messageBytes);

                item.Value.WriteAndFlushAsync(initialMessage);
            }
        }
    }
}
