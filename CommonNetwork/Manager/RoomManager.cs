using System;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Collections.Concurrent;
using CommonLibs;

namespace CommonNetwork
{
    public class RoomManager<T> : IRoomManager where T : RoomBase
    {
        private readonly IServiceProvider m_services;
        private ConcurrentDictionary<int, T> m_rooms;
        private ConcurrentQueue<UserData> m_waitingList;

        private int m_nextId = 0;

        public RoomManager(IServiceProvider services)
        {
            m_services = services;
            m_rooms = new ConcurrentDictionary<int, T>();
            m_waitingList = new ConcurrentQueue<UserData>();
        }

        public RoomBase CreateRoom(UserData userData)
        {
            var id = Interlocked.Increment(ref m_nextId);
            var rm = (T)Activator.CreateInstance(typeof(T), id, m_services);
            if (m_rooms.TryAdd(id, rm))
            {
                rm.Enter(userData);
            }
            return rm;
        }

        public RoomBase GetRoom(int id)
        {
            m_rooms.TryGetValue(id, out T rm);
            return rm;
        }

        public bool RemoveRoom(int id)
        {
            bool ret = false;
            if (m_rooms.TryRemove(id, out T rm))
            {
                ret = true;
                rm.Close();
            }
            return ret;
        }

        /// <summary>
        /// 玩家进入搜索等待列表
        /// </summary>
        /// <param name="userData"></param>
        /// <returns></returns>
        public bool AddToWaitingList(UserData userData)
        {
            bool ret = true;
            var wlist = m_waitingList.ToArray();
            for (int i = 0; i < wlist.Length; i++)
            {
                if (wlist[i].ID == userData.ID)
                {
                    //ret = false;
                    break;
                }
            }
            if (ret)
            {
                if (m_waitingList.TryDequeue(out UserData hoster))
                {
                    Task task = new Task(() => InitialRoom(hoster, userData));
                    task.Start();
                }
                else
                {
                    m_waitingList.Enqueue(userData);
                }
            }
            return ret;
        }

        private void InitialRoom(UserData hoster, UserData other)
        {
            var rm = CreateRoom(hoster);
            if (rm != null)
            {
                rm.Enter(other);
                rm.BeginPrepare();
            }
        }
    }
}
