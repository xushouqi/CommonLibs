using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommonLibs;

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

    public abstract class RoomBase
    {
        public int ID = 0;
        protected RoomStateEnum State = RoomStateEnum.None;
        protected int Turn = 0;
        protected int TurnInterval = 66; //每Turn多少毫秒
        protected int BufferSize = 1024;
        protected int PrepareTime = 10000; //准备时长
        protected int GameMaxTime = 3600000; //游戏最大时长
        protected int EndShowTime = 10000; //结束后停留时长

        protected int m_total_tickcount = 0;
        protected int m_last_tickcount = 0;

        protected Task m_updateTask;

        protected UserData m_hoster = null;
        protected SortedDictionary<int, UserData> m_members;
        protected List<WebPackage> m_updatas;
        protected List<WebPackage> m_pushdatas;
        protected readonly UserSocketManager m_userSocketManager;
        protected readonly Assembly m_assembly;
        protected readonly string m_project_name;
        protected readonly IServiceProvider m_services;
        protected readonly ILogger m_logger;
        protected readonly IUserManager<UserData> m_userManager;

        public RoomBase(int id, IServiceProvider services)
        {
            ID = id;
            m_services = services;
            m_userSocketManager = m_services.GetService<UserSocketManager>();
            m_assembly = Assembly.GetEntryAssembly();
            m_project_name = m_assembly.FullName.Split(',')[0];
            m_logger = services.GetService<ILoggerFactory>().CreateLogger("RoomService");
            m_userManager = services.GetService<IUserManager<UserData>>();

            m_members = new SortedDictionary<int, UserData>();
            m_updatas = new List<WebPackage>();
            m_pushdatas = new List<WebPackage>();

            State = RoomStateEnum.None;

            m_userManager.AddOnRemoveUser(OnRemoveUser);
        }

        void OnRemoveUser(int uid)
        {
            if (m_members.ContainsKey(uid))
            {
                Leave(uid);
            }
        }

        public void Enter(UserData userData)
        {
            if (State == RoomStateEnum.None)
            {
                lock (m_members)
                {
                    if (!m_members.ContainsKey(userData.ID))
                    {
                        if (m_hoster == null)
                            m_hoster = userData;

                        m_members.Add(userData.ID, userData);
                    }
                }

                //下发客户端房间数据
                //通知房间其他人有人进来了
                OnEnter(userData);
            }
        }
        protected virtual void OnEnter(UserData userData)
        {

        }
        public void Leave(int uid)
        {
            UserData userData = null;
            lock (m_members)
            {
                if (m_members.TryGetValue(uid, out userData))
                {
                    m_members.Remove(uid);

                    if (m_hoster != null && m_hoster.ID == uid)
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

                    //没人了，删除房间
                    if (m_members.Count == 0)
                        Close();
                }
            }

            //通知其他人有人离开了
            if (userData != null && State != RoomStateEnum.Closed)
                OnLeave(userData);
        }
        protected virtual void OnLeave(UserData userData)
        {

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

        public bool BeginPrepare()
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
                OnBeginPrepare();
            }
            return ret;
        }
        protected virtual void OnBeginPrepare()
        {

        }

        public bool StartGame()
        {
            bool ret = false;
            if (State == RoomStateEnum.Prepare)
            {
                State = RoomStateEnum.Start;
                ret = true;
                //开始计时
                m_total_tickcount = 0;
                m_last_tickcount = 0;
                OnStartGame();
            }
            return ret;
        }
        protected virtual void OnStartGame()
        {

        }

        public bool EndGame()
        {
            bool ret = false;
            if (State == RoomStateEnum.Start)
            {
                State = RoomStateEnum.End;
                ret = true;
                //开始计时
                m_total_tickcount = 0;
                m_last_tickcount = 0;
                OnEndGame();
            }
            return ret;
        }
        protected virtual void OnEndGame()
        {

        }
        public void Close()
        {
            if (State != RoomStateEnum.Closed)
            {
                State = RoomStateEnum.Closed;
                OnClose();
                //todo: destroy
            }
        }
        protected virtual void OnClose()
        {

        }

        private void UpdateTask()
        {
            while(true)
            {
                int curtick = Environment.TickCount;
                int delta = curtick - m_last_tickcount;
                if (m_last_tickcount == 0 || delta >= TurnInterval)
                {
                    if (m_last_tickcount > 0)
                        m_total_tickcount += delta;
                    m_last_tickcount = curtick;

                    if (State == RoomStateEnum.Prepare)
                    {
                        OnUpdatePrepare();
                    }
                    else if (State == RoomStateEnum.Start)
                    {
                        OnUpdateTurn();
                    }
                    else if (State == RoomStateEnum.End)
                    {
                        OnUpdateEnd();
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

        protected virtual void OnUpdatePrepare()
        {
            if (m_total_tickcount >= PrepareTime)
            {
                StartGame();
            }
        }
        protected virtual void OnUpdateTurn()
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
                                    retPackage.ID = ID * 2 + Turn * 1000 + i + 1;
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

            //超出最大时长，结束
            if (m_total_tickcount >= GameMaxTime)
                EndGame();
        }

        protected virtual void OnUpdateEnd()
        {
            if (m_total_tickcount >= EndShowTime)
            {
                Close();
            }
        }

        protected void PushToClient(UserData userData, WebPackage[] datas)
        {
            for (int i = 0; i < datas.Length; i++)
            {
                var package = datas[i].ShallowCopy();
                package.Uid = userData.ID;
                m_userSocketManager.SendPackageToUserAsync(package).Wait();
            }
        }
    }
}
