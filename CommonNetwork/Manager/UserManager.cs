using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using CommonLibs;

namespace CommonNetwork
{
    public class UserManager<T> : IUserManager<T> where T:UserData
    {
        ConcurrentDictionary<string, int> m_useridByChannel = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<int, T> m_usersById = new ConcurrentDictionary<int, T>();

        object m_lock = new object();

        public int GetSocketUserCount()
        {
            return m_useridByChannel.Count;
        }
        public int GetTotalUserCount()
        {
            return m_usersById.Count;
        }

        public virtual T UpdateUser(UserConnTypeEnum connType, string channel, int userId, UserTypeEnum userType, int roleId, string jti, double expiresIn)
        {
            T data = default(T);
            if (userId > 0)
            {
                lock (m_lock)
                {
                    if (!string.IsNullOrEmpty(channel))
                    {
                        m_useridByChannel.AddOrUpdate(channel, userId, (key, oldValue) => userId);
                    }

                    if (m_usersById.TryGetValue(userId, out data))
                    {
                        data.Type = userType;
                        data.RoleId = roleId;
                        data.Channel = channel;
                        data.ConnType = connType;
                        data.Jti = jti;
                        data.ExpireTime = DateTime.Now.AddSeconds(expiresIn);
                    }
                    else
                    {
                        data = (T)Activator.CreateInstance(typeof(T));
                        data.ID = userId;
                        data.Type = userType;
                        data.RoleId = roleId;
                        data.Channel = channel;
                        data.ConnType = connType;
                        data.Jti = jti;
                        data.ExpireTime = DateTime.Now.AddSeconds(expiresIn);
                        m_usersById.TryAdd(userId, data);

                        var explist = m_usersById.Where(t => t.Value.ExpireTime < DateTime.Now).ToArray();
                        for (int i = 0; i < explist.Length; i++)
                        {
                            m_usersById.TryRemove(explist[i].Key, out T value);
                        }
                    }
                }
            }
            return data;
        }
        
        public int RemoveUser(string channel)
        {
            int id = 0;
            lock (m_lock)
            {
                if (m_useridByChannel.TryGetValue(channel, out id))
                {
                    m_usersById.TryRemove(id, out T data);
                    m_useridByChannel.TryRemove(channel, out int tid);
                }
            }
            return id;
        }
        public virtual bool RemoveUserById(int id)
        {
            bool ret = false;
            lock (m_lock)
            {
                T data = null;
                if (m_usersById.TryGetValue(id, out data))
                {
                    ret = true;
                    if (!string.IsNullOrEmpty(data.Channel))
                        m_useridByChannel.TryRemove(data.Channel, out int tid);
                    m_usersById.TryRemove(id, out T tdata);
                }
            }
            return ret;
        }

        public bool ValidChannel(string channel)
        {
            return m_useridByChannel.ContainsKey(channel);
        }
        public virtual T GetUserDataById(int id)
        {
            T user = null;
            m_usersById.TryGetValue(id, out user);
            return user;
        }

        public T GetUserData(string channel)
        {
            T user = null;
            m_useridByChannel.TryGetValue(channel, out int id);
            if (id > 0)
                m_usersById.TryGetValue(id, out user);
            return user;
        }
    }
}
