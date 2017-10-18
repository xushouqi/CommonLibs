using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommonLibs;
using ProtoBuf;

namespace CommonNetwork
{
    public enum RoomStateEnum
    {
        None = 0,
        Prepare,
        Start,
        End,
        Closed,
    }

    public class RoomBase
    {
        public int ID = 0;
        protected RoomStateEnum State = RoomStateEnum.None;
        protected int Turn = 0;
        protected int TurnInterval = 66; //每Turn多少毫秒
        protected int BufferSize = 1024;
        protected int PrepareTime = 10000; //准备时长

        private int m_total_tickcount = 0;
        private int m_last_tickcount = 0;

        private Task m_updateTask;

        protected UserData m_hoster = null;
        protected Dictionary<int, UserData> m_members;
        protected List<WebPackage> m_updatas;
        protected List<WebPackage> m_pushdatas;
        protected readonly UserSocketManager m_userSocketManager;
        private readonly Assembly m_assembly;
        private readonly string m_project_name;
        private readonly IServiceProvider m_services;
        private readonly ILogger m_logger;
        private readonly IUserManager<UserData> m_userManager;
        private readonly IRoomManager m_roomManager;

        public RoomBase(int id, IServiceProvider services, IRoomManager roomManager)
        {
            ID = id;
            m_services = services;
            m_userSocketManager = m_services.GetService<UserSocketManager>();
            m_assembly = Assembly.GetEntryAssembly();
            m_project_name = m_assembly.FullName.Split(',')[0];
            m_logger = services.GetService<ILoggerFactory>().CreateLogger("RoomService");
            m_userManager = services.GetService<IUserManager<UserData>>();
            m_roomManager = roomManager;

            m_members = new Dictionary<int, UserData>();
            m_updatas = new List<WebPackage>();
            m_pushdatas = new List<WebPackage>();

            State = RoomStateEnum.None;
        }

        public virtual void Enter(UserData userData)
        {
            if (State == RoomStateEnum.None)
            {
                lock (m_members)
                {
                    if (m_hoster == null)
                        m_hoster = userData;

                    m_members.Add(userData.ID, userData);
                }

                //下发客户端房间数据


                //通知房间其他人有人进来了
            }
        }
        public virtual void Leave(UserData userData)
        {
            lock (m_members)
            {
                if (m_members.ContainsKey(userData.ID))
                    m_members.Remove(userData.ID);

                if (m_hoster == userData)
                {
                    if (m_members.Count > 0)
                    {
                        foreach (var item in m_members)
                        {
                            m_hoster = item.Value;
                            break;
                        }
                    }
                    else
                        m_hoster = null;
                }

                //通知其他人有人离开了

            }
        }

        /// <summary>
        /// 游戏进行中，受到客户端提交的操作数据
        /// </summary>
        /// <param name="package"></param>
        public void ReceiveGameData(WebPackage package)
        {
            if (State == RoomStateEnum.Start)
            {
                if (m_members.ContainsKey(package.Uid))
                {
                    lock (m_updatas)
                    {
                        if (package.Turn == Turn)
                            m_updatas.Add(package);
                    }
                }
            }
        }

        public virtual bool BeginPrepare()
        {
            bool ret = false;
            if (State == RoomStateEnum.None)
            {
                State = RoomStateEnum.Prepare;
                ret = true;
                //开始计时
                m_total_tickcount = 0;
                m_last_tickcount = 0;

                m_updateTask = new Task(UpdateTask);
                m_updateTask.Start();
            }
            return ret;
        }
        public virtual bool StartGame()
        {
            bool ret = false;
            if (State == RoomStateEnum.Prepare)
            {
                State = RoomStateEnum.Start;
                ret = true;
                //开始计时
                m_total_tickcount = 0;
                m_last_tickcount = 0;
            }
            return ret;
        }
        public virtual bool EndGame()
        {
            bool ret = false;
            if (State == RoomStateEnum.Start)
            {
                State = RoomStateEnum.End;
                ret = true;
            }
            return ret;
        }
        public virtual void Close()
        {
            State = RoomStateEnum.Closed;
        }

        private void UpdateTask()
        {
            while(true)
            {
                int curtick = Environment.TickCount;
                int delta = curtick - m_last_tickcount;
                if (m_last_tickcount == 0 || delta >= TurnInterval)
                {
                    m_last_tickcount = curtick;
                    if (m_last_tickcount > 0)
                        m_total_tickcount += delta;

                    if (State == RoomStateEnum.Prepare)
                    {
                        UpdatePrepare();
                    }
                    else if (State == RoomStateEnum.Start)
                    {
                        UpdateTurn();
                    }
                    else
                        break;
                }

                //处理总共花了多长时间
                int duration = Environment.TickCount - curtick;
                if (duration < TurnInterval)
                    Task.Delay(TurnInterval - duration).Wait();
            }
        }

        protected virtual void UpdatePrepare()
        {
            if (m_total_tickcount >= PrepareTime)
            {
                StartGame();
            }
        }

        private void UpdateTurn()
        {
            Turn++;

            //处理所有输入
            lock (m_updatas)
            {
                for (int i = 0; i < m_updatas.Count; i++)
                {
                    var package = m_updatas[i];
                    if (m_members.TryGetValue(package.Uid, out UserData userData))
                    {
                        //调用对应的服务
                        var actionName = string.Concat(m_project_name, ".Actions.Action", package.ActionId);

                        try
                        {
                            Type atype = m_assembly.GetType(actionName);
                            if (atype != null)
                            {
                                var action = (IAction)m_services.GetService(atype);
                                if (action != null)
                                {
                                    //提交参数
                                    action.Submit(userData.Channel, UserConnTypeEnum.Tcp, userData.ID, package);
                                    //执行
                                    action.DoAction().Wait();
                                    //获取返回值
                                    var retPackage = action.GetReturnPackage();
                                    retPackage.ID = ID * 10000 + Turn * 1000 + i + 1;
                                    //加入待下发列表
                                    m_pushdatas.Add(retPackage);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            m_logger.LogError("RoomService.Update: {0}\n{1}", e.Message, e.StackTrace);
                        }
                    }
                }
                m_updatas.Clear();
            }

            //下发数据
            if (m_pushdatas.Count > 0)
            {
                var datas = m_pushdatas.ToArray();
                m_pushdatas.Clear();

                List<Task> tasklist = new List<Task>();
                lock (m_members)
                {
                    for (int i = 0; i < m_members.Count; i++)
                    {
                        var member = m_members[i];
                        Task task = Task.Factory.StartNew(() => PushToClient(member, datas));
                        tasklist.Add(task);
                    }
                }
                Task.WaitAll(tasklist.ToArray());
            }
        }

        protected void PushToClient(UserData userData, WebPackage[] datas)
        {
            for (int i = 0; i < datas.Length; i++)
            {
                var package = datas[i].ShallowCopy();
                package.Uid = userData.ID;
                m_userSocketManager.SendPackageToUser(package).Wait();
            }
        }
    }
}
