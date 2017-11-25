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

    /// <summary>
    /// 管道处理基类，较常用
    /// </summary>
    public class NettySocketHander : ChannelHandlerAdapter
    {
        private readonly UserSocketManager m_userSocketManager;
        private readonly Assembly m_assembly;
        private readonly string m_project_name;
        private readonly ILogger m_logger;
        private readonly IServiceProvider m_services;
        private readonly IUserManager<UserData> m_userManager;
        private readonly IPushManager m_pushManager;
        private readonly IRoomManager m_roomManager;
        private readonly NettyOptions m_config;
        private readonly DedicatedThreadPool m_threadPool;

        public NettySocketHander(IServiceProvider services, NettyOptions config, NettyServer nettyHandler)
        {
            m_services = services;
            m_config = config;
            m_assembly = Assembly.GetEntryAssembly();
            m_project_name = m_assembly.FullName.Split(',')[0];
            m_userSocketManager = services.GetService<UserSocketManager>();
            m_logger = services.GetService<ILoggerFactory>().CreateLogger("NettyServerHandler");
            m_userManager = services.GetService<IUserManager<UserData>>();
            m_pushManager = services.GetService<IPushManager>();
            m_roomManager = services.GetService<IRoomManager>();
            m_threadPool = services.GetService<DedicatedThreadPool>();
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            m_userSocketManager.Set(context);
            base.ChannelActive(context);
        }
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            m_userSocketManager.Remove(context);
            //移除user
            int id = m_userManager.RemoveUser(context.Channel.Id.ToString());
        }

        /// <summary>
        /// 重写基类的方法，当消息到达时触发
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            m_logger.LogInformation("ChannelRead: {0}", context.Channel.Id);

            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                try
                {
                    var package = ProtoBufUtils.Deserialize<WebPackage>(buffer.Array);
                    if (package != null)
                    {
                        //房间游戏数据，交给房间处理
                        if (package.Room > 0)
                        {
                            var rmm = m_roomManager.GetRoom(package.Room);
                            if (rmm != null)
                            {
                                rmm.ReceiveGameData(package);
                            }
                            else
                                m_logger.LogError("Room:{0} NOT Exists!!! uid={1}", package.Room, package.Uid);
                        }
                        else
                            m_threadPool.QueueUserWorkItem(() => DoReceive(package, context.Channel.Id.ToString()).Wait());
                    }
                }
                catch (Exception e)
                {
                    m_logger.LogError("ChannelRead Error: {0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

        private async Task DoReceive(WebPackage package, string channel)
        {
            //调用对应的服务
            var actionName = string.Concat(m_project_name, ".Actions.Action", package.ActionId);
            m_logger.LogInformation("DoReceive actionName={0}, Thread={1}", actionName, Thread.CurrentThread.ManagedThreadId);

            try
            {
                Type atype = m_assembly.GetType(actionName);
                if (atype != null)
                {
                    var action = (IAction)m_services.GetService(atype);
                    if (action != null)
                    {
                        int uid = 0;
                        var user = m_userManager.GetUserData(channel);
                        if (user != null)
                        {
                            //注册下发数据
                            m_pushManager.AddPushAction(user, PushToClient);

                            if (package.Uid <= 0 || package.Uid == user.ID)
                                uid = user.ID;
                        }

                        bool validGo = true;
                        AuthPolicyAttribute attri = null;
                        //需验证用户身份权限
                        if (atype.GetTypeInfo().IsDefined(typeof(AuthPolicyAttribute), false))
                        {
                            attri = (AuthPolicyAttribute)atype.GetTypeInfo().GetCustomAttribute(typeof(AuthPolicyAttribute), false);
                            if (attri.AuthPolicy > UserTypeEnum.None)
                                validGo = user != null && user.Type >= attri.AuthPolicy;
                        }

                        if (uid > 0 || (attri != null && attri.AuthPolicy == UserTypeEnum.None))
                        {
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                            WebPackage result = null;
                            if (validGo)
                            {
                                //提交参数
                                if (user != null)
                                    action.Submit(channel, user, package);
                                else
                                    action.Submit(channel, UserConnTypeEnum.Tcp, uid, package);

                                //执行
                                sw.Start();
                                await action.DoAction();
                                sw.Stop();

                                //获取返回值
                                result = action.GetReturnPackage();
                            }
                            else
                                result = action.GetUnAuthorizedPackage(package);

                            m_logger.LogInformation("NettyAct: {0}, thread={1}, DoActionTime: {2}ms",
                                actionName, Thread.CurrentThread.ManagedThreadId, sw.Elapsed.TotalMilliseconds);

                            //其他后续操作
                            await action.AfterAction();
                        }
                        else
                        {
                            package.ErrorCode = ErrorCodeEnum.NotValid;
                            m_logger.LogError("Uid NOT Found!!! {0}", actionName);
                        }
                    }
                    else
                    {
                        package.ErrorCode = ErrorCodeEnum.NoAction;
                        m_logger.LogError("{0} NOT Found!!!", actionName);
                    }
                }
                else
                {
                    package.ErrorCode = ErrorCodeEnum.NoAction;
                    m_logger.LogError("{0} NOT Found!!!", actionName);
                }

                //回包
                await SendAsync(channel, package);
            }
            catch (Exception e)
            {
                m_logger.LogError("DoReceive.Exception: {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        private async void PushToClient(WebPackage package)
        {
            await m_userSocketManager.SendPackageToUserAsync(package);
        }
        private async Task SendAsync(string channel, WebPackage package)
        {
            m_logger.LogInformation("SendAsync thread={0}, pid={1}, channel={1}", Thread.CurrentThread.ManagedThreadId, package.ID, channel);
            await m_userSocketManager.SendPackageToUserAsync(UserConnTypeEnum.Tcp, channel, package);
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
            m_logger.LogError("Exception: " + exception);
            context.CloseAsync();
        }
    }
}
