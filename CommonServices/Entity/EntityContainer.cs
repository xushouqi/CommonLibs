using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using CommonLibs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CommonServices
{
    public enum ContainerStateEnum
    {
        None = 0,
        Loading = 1,
        Ready,
    }

    public  class EntityContainer<T, TContext> : IDisposable where T: Entity where TContext : DbContext
    {
        private readonly RedisClient m_redisClient;
        private readonly ILogger m_logger;
        private readonly TContext m_context;

        private readonly ConcurrentDictionary<int, T> m_cacheStruct;
        //private readonly ConcurrentDictionary<int, T> m_addDb;
        private readonly ConcurrentDictionary<int, T> m_updateDb;
        private readonly ConcurrentBag<T> m_waitToDb;
        //private readonly ConcurrentDictionary<int, T> m_removeDb;

        //自增的ID
        private int m_sequenceId;
        private ContainerStateEnum m_state = ContainerStateEnum.None;

        public bool AllInMem = false;

        private object m_lock_data;
        private DateTime m_lastSaveDbTime;

        private readonly int MinToDbCount = 10;
        private readonly int MillisecSaveInterval = 500;

        public EntityContainer(RedisClient redisClient,
                            ILoggerFactory logFactory,
                            TContext context
                            )
        {
            Type type = typeof(T);
            m_redisClient = redisClient;
            m_logger = logFactory.CreateLogger($"EntityContainer.{type.Name}");
            m_cacheStruct = new ConcurrentDictionary<int, T>();
            m_updateDb = new ConcurrentDictionary<int, T>();
            m_waitToDb = new ConcurrentBag<T>();
            m_context = context;

            m_lock_data = new object();
            m_lastSaveDbTime = DateTime.MinValue;

            DataModelsAttribute attrib = (DataModelsAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(DataModelsAttribute), false);
            if (attrib != null)
            {
                AllInMem = attrib.AllInMem;
            }

            Task.Run(TrySaveDb);

            Initial();
        }

        /// <summary>
        /// 从数据库全加载
        /// </summary>
        protected void Initial()
        {
            if (AllInMem)
            {
                m_sequenceId = 0;
                m_context.Set<T>()
                .AsNoTracking()
                .ForEach((data) =>
                {
                    int id = data.GetId();
                    m_cacheStruct.AddOrUpdate(id, data, (key, oldValue) => oldValue = data);
                    if (id > m_sequenceId)
                        m_sequenceId = id;
                });
            }
            else
            {
                //从数据库获取当前最大的ID
                m_sequenceId = m_context.Set<T>().CountAsync().GetAwaiter().GetResult() > 0 ?
                    m_context.Set<T>().MaxAsync(t => t.GetId()).GetAwaiter().GetResult() : 0;
            }
            m_state = ContainerStateEnum.Ready;
        }

        void IDisposable.Dispose()
        {
            DoSaveDb().Wait();
        }

        public void Reload()
        {
            m_state = ContainerStateEnum.Loading;
            Clear();
            Initial();
        }

        private void CheckReady()
        {
            if (m_state != ContainerStateEnum.Ready)
                throw new NotSupportedException($"EntityContainer: {typeof(T).FullName} NOT ready.");
        }
        private int GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref m_sequenceId);
        }

        public T this[int id]
        {
            get
            {
                T data = default(T);
                TryGetValue(id, out data);
                return data;
            }
            set
            {
                Add(value);
            }
        }

        public ICollection<int> Keys
        {
            get
            {
                return m_cacheStruct.Keys;
            }
        }

        public ICollection<T> Values
        {
            get
            {
                return m_cacheStruct.Values;
            }
        }

        public int Count
        {
            get
            {
                return m_cacheStruct.Count;
            }
        }

        public bool TryGetValue(int id, out T value)
        {
            CheckReady();
            bool ret = m_cacheStruct.TryGetValue(id, out value);
            if (!ret)
            {
                //从数据库中获取
                var data = m_context.Set<T>().Find(id);
                if (data != null)
                {
                    //不追踪修改状态
                    m_context.Entry(data).State = EntityState.Detached;
                    //添加到缓存
                    m_cacheStruct.AddOrUpdate(id, data, (key, oldValue) => oldValue = data);
                    value = data;
                    ret = true;
                }
            }
            return ret;
        }

        /// <summary>
        /// 新增对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void Add(T data)
        {
            CheckReady();
            int id = data.GetId();
            if (id <= 0)
            {
                id = GetNextId();
                data.SetId(id);
            }
            //加入缓存
            m_cacheStruct.AddOrUpdate(id, data, (key, oldValue) => oldValue = data);

            //存入数据库
            SaveDbAsync(data, EntityActionEnum.Add);
            //Task.Run(() => AddAsync(data));
        }

        public bool Update(T data)
        {
            CheckReady();
            if (TryGetValue(data.GetId(), out T value))
            {
                data.TryUpdateTime();
                SaveDbAsync(data, EntityActionEnum.Update);
                //Task.Run(() => UpdateAsync(data));
                return true;
            }
            return false;
        }

        public bool Remove(int id)
        {
            CheckReady();
            if (TryGetValue(id, out T data))
            {
                SaveDbAsync(data, EntityActionEnum.Remove);
                //Task.Run(() => RemoveAsync(data));

                m_cacheStruct.TryRemove(id, out data);
                return true;
            }
            return false;
        }

        private void SaveDbAsync(T data, EntityActionEnum action)
        {
            if (action != EntityActionEnum.Update)
                data.Action = action;
            if (m_updateDb.TryAdd(data.GetId(), data))
            {
                data.Action = action;
            }
        }

        private async Task TrySaveDb()
        {
            while (true)
            {
                if (m_updateDb.Count >= MinToDbCount
                    || MathUtils.DiffDate(m_lastSaveDbTime).TotalMilliseconds >= MillisecSaveInterval
                    )
                {
                    await DoSaveDb();
                    m_lastSaveDbTime = DateTime.Now;
                }
                Task.Delay(MillisecSaveInterval).Wait();
            }
        }
        private async Task DoSaveDb()
        {
            if (m_updateDb.Count >= 0)
            {
                m_updateDb.ForEach((data) =>
                {
                    if (data.Value.Action == EntityActionEnum.Add)
                        m_context.Add(data.Value);
                    else if (data.Value.Action == EntityActionEnum.Remove)
                        m_context.Remove(data.Value);
                    else
                        m_context.Update(data.Value);
                });
                m_updateDb.Clear();
                await m_context.SaveChangesAsync();
            }
        }

        public T Refresh(int id)
        {
            CheckReady();
            m_cacheStruct.TryRemove(id, out T data);
            TryGetValue(id, out data);
            return data;
        }

        public void Clear()
        {
            m_cacheStruct.Clear();
        }

        public bool ContainsKey(int id)
        {
            return TryGetValue(id, out T data);
        }

        public List<T> WhereToList<TKey>(Func<T, bool> predicate)
        {
            List<T> datalist = null;
            if (AllInMem)
            {
                lock (m_lock_data)
                {
                    datalist = new List<T>();
                    m_cacheStruct.ForEach((data) =>
                    {
                        if (predicate(data.Value))
                            datalist.Add(data.Value);
                    });
                }
            }
            else
            {
                datalist = m_context.Set<T>().Where(predicate).ToList();
            }
            return datalist;
        }
        public T FirstOrDefault(Func<T, bool> predicate)
        {
            if (AllInMem)
            {
                var data = m_cacheStruct.FirstOrDefault((item) => predicate(item.Value));
                return data.Value ?? default(T);
            }
            else
            {
                return m_context.Set<T>().FirstOrDefault(predicate);
            }
        }
        public bool Any(Func<T, bool> predicate)
        {
            if (AllInMem)
            {
                return m_cacheStruct.Any((item) => predicate(item.Value));
            }
            else
            {
                return m_context.Set<T>().Any(predicate);
            }
        }

    }
}
