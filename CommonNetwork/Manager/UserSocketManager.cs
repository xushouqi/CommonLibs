using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.Extensions.Options;
using DotNetty.Transport.Channels;
using DotNetty.Buffers;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{
    public class UserSocketManager
    {
        private ConcurrentDictionary<string, IChannelHandlerContext> m_contexts;
        private ConcurrentDictionary<string, WebSocket> m_socketsByChannel;
        private IUserManager<UserData> m_userManager;

        public UserSocketManager(IUserManager<UserData> userManager)
        {
            m_userManager = userManager;
            m_contexts = new ConcurrentDictionary<string, IChannelHandlerContext>();
            m_socketsByChannel = new ConcurrentDictionary<string, WebSocket>();
        }

        public void Set(IChannelHandlerContext context)
        {
            string channel = context.Channel.Id.ToString();
            m_contexts.AddOrUpdate(channel, context, (key, oldValue) => context);
        }
        public void Remove(IChannelHandlerContext context)
        {
            string channel = context.Channel.Id.ToString();
            m_contexts.TryRemove(channel, out context);
        }
        public void Set(WebSocket socket)
        {
            string channel = socket.GetHashCode().ToString();
            m_socketsByChannel.AddOrUpdate(channel, socket, (key, oldValue) => socket);
        }
        public void Remove(WebSocket socket)
        {
            string channel = socket.GetHashCode().ToString();
            m_socketsByChannel.TryRemove(channel, out socket);
        }

        public async Task<bool> SendPackageToUserAsync(UserConnTypeEnum connType, string channel, byte[] bytes)
        {
            bool ret = false;
            if (connType == UserConnTypeEnum.WebSocket
                && m_socketsByChannel.TryGetValue(channel, out WebSocket socket))
            {
                var ia = new ArraySegment<byte>(bytes);
                await socket.SendAsync(ia, WebSocketMessageType.Binary, true, new CancellationTokenSource(60000).Token);
                ret = true;
            }
            else if (connType == UserConnTypeEnum.Tcp
                && m_contexts.TryGetValue(channel, out IChannelHandlerContext context))
            {
                var msg = Unpooled.Buffer(bytes.Length, 4096);
                msg.WriteBytes(bytes);
                await context.WriteAndFlushAsync(msg);
                ret = true;
            }
            return ret;
        }
        public async Task<bool> SendPackageToUserAsync(UserConnTypeEnum connType, string channel, WebPackage package)
        {
            byte[] bytes = ProtoBufUtils.Serialize(package);
            bool ret = await SendPackageToUserAsync(connType, channel, bytes);
            return ret;
        }
        public async Task<bool> SendPackageToUserAsync(WebPackage package)
        {
            bool ret = false;
            var userData = m_userManager.GetUserDataById(package.Uid);
            if (userData != null)
            {
                byte[] bytes = ProtoBufUtils.Serialize(package);
                ret = await SendPackageToUserAsync(userData, bytes);
            }
            return ret;
        }
        public async Task<bool> SendPackageToUserAsync(UserData userData, WebPackage package)
        {
            byte[] bytes = ProtoBufUtils.Serialize(package);
            bool ret = await SendPackageToUserAsync(userData, bytes);
            return ret;
        }
        public async Task<bool> SendPackageToUserAsync(int uid, byte[] bytes)
        {
            bool ret = false;
            var userData = m_userManager.GetUserDataById(uid);
            if (userData != null)
            {
                ret = await SendPackageToUserAsync(userData, bytes);
            }
            return ret;
        }
        public async Task<bool> SendPackageToUserAsync(UserData userData, byte[] bytes)
        {
            bool ret = false;
            if (userData != null)
            {
                ret = await SendPackageToUserAsync(userData.ConnType, userData.Channel, bytes);
            }
            return ret;
        }
    }
}
