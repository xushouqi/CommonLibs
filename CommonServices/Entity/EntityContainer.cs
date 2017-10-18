using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using CommonLibs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace CommonServices
{
    public enum ContainerStateEnum
    {
        None = 0,
        Loading = 1,
        Ready,
    }

    public  class EntityContainer<TKey, TValue, TContext> : IDisposable where TValue: Entity<TKey> where TContext: DbContext
    {
        //private readonly RedisClient m_redisClient;
        private readonly IDatabase m_redisDb;
        private readonly ILogger m_logger;
        private readonly TContext m_context;

        //private readonly ConcurrentDictionary<int, TValue> m_cacheStruct;
        private readonly SizeLimitedCache<TKey, TValue> m_cacheStruct;
        private readonly ConcurrentDictionary<TKey, TValue> m_updateDb;

        //自增的ID
        private int m_sequenceId;
        private ContainerStateEnum m_state = ContainerStateEnum.None;

        public DataModelsAttribute DataAttribs;

        private object m_lock_data;
        private DateTime m_lastSaveDbTime;

        private readonly int MinToDbCount = 10;
        private readonly int MillisecSaveInterval = 500;
        private readonly string EntityTypeName;

        public EntityContainer(
                            ILoggerFactory logFactory,
                            TContext context,
                            RedisClient redisClient = null,
                            int MaxCacheSize = 1000
                            )
        {
            Type type = typeof(TValue);
            EntityTypeName = type.Name;
            //m_redisClient = redisClient;
            //m_redisDb = redisClient.GetDatabase("Entity");
            m_logger = logFactory.CreateLogger($"EntityContainer.{EntityTypeName}");
            m_cacheStruct = new SizeLimitedCache<TKey, TValue>(MaxCacheSize);
            m_updateDb = new ConcurrentDictionary<TKey, TValue>();
            m_context = context;

            m_lock_data = new object();
            m_lastSaveDbTime = DateTime.MinValue;

            DataAttribs = (DataModelsAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(DataModelsAttribute), false);

            Task.Run(TrySaveDb);

            Initial();
        }

        /// <summary>
        /// 从数据库全加载
        /// </summary>
        protected void Initial()
        {
            if (DataAttribs.LoadInCache)
            {
                m_sequenceId = 0;
                var datalist = m_context.Set<TValue>().AsNoTracking();

                datalist.ForEach((data) =>
                {
                    m_cacheStruct.AddOrUpdate(data.Key, data, (key, oldValue) => oldValue = data);
                    //载入redis
                    if (m_redisDb != null)
                        m_redisDb.HashSet(EntityTypeName, data.Key.ToRedisValue(), data.ToRedisValue());

                    if (DataAttribs.IncrementKey)
                        m_sequenceId = data.Key.ToType<int>();
                });
            }
            else
            {
                //从数据库获取当前最大的ID
                if (DataAttribs.IncrementKey)
                {
                    int count = m_context.Set<TValue>().CountAsync().GetAwaiter().GetResult();
                    m_sequenceId = count > 0 ? m_context.Set<TValue>().MaxAsync(t => t.Key.ToType<int>()).GetAwaiter().GetResult() : 0;
                }
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
                throw new NotSupportedException($"EntityContainer: {typeof(TValue).FullName} NOT ready.");
        }
        private int GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref m_sequenceId);
        }

        public TValue this[TKey key]
        {
            get
            {
                return Find(key);
            }
            set
            {
                Add(value);
            }
        }
        public TValue Find(TKey key)
        {
            TValue data = default(TValue);
            TryGetValue(key, out data);
            return data;
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (DataAttribs.LoadInCache)
                {
                    if (m_redisDb != null)
                        return m_redisDb.HashKeys(EntityTypeName).Select(key => key.ToValueOfType<TKey>()).ToArray();
                    else
                        return m_cacheStruct.Keys;
                }
                else
                {
                    return m_context.Set<TValue>().Select(t=>t.Key).ToArray();
                }
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                if (DataAttribs.LoadInCache)
                {
                    if (m_redisDb != null)
                        return m_redisDb.HashValues(EntityTypeName).Select(t => t.ToValueOfType<TValue>()).ToArray();
                    else
                        return m_cacheStruct.Values;
                }
                else
                {
                    return m_context.Set<TValue>().AsNoTracking().ToArray();
                }
            }
        }

        public long Count
        {
            get
            {
                if (DataAttribs.LoadInCache)
                {
                    if (m_redisDb != null)
                        return m_redisDb.HashLength(EntityTypeName);
                    else
                        return m_cacheStruct.Count;
                }
                else
                {
                    return m_context.Set<TValue>().Count();
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            CheckReady();
            bool ret = m_cacheStruct.TryGetValue(key, out value);
            if (!ret)
            {
                TValue data = default(TValue);
                //从redis中获取
                if (m_redisDb != null)
                    data = m_redisDb.HashGet(EntityTypeName, key.ToRedisValue()).ToValueOfType<TValue>();
                //从数据库中获取
                if (data == null)
                    data = m_context.Set<TValue>().Find(key);
                else if (m_redisDb != null)
                    //载入redis
                    m_redisDb.HashSet(EntityTypeName, key.ToRedisValue(), data.ToRedisValue());

                if (data != null)
                {
                    //不追踪修改状态
                    m_context.Entry(data).State = EntityState.Detached;
                    //添加到缓存
                    m_cacheStruct.AddOrUpdate(key, data, (oldkey, oldValue) => oldValue = data);
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
        public void Add(TValue data)
        {
            CheckReady();
            if (DataAttribs.IncrementKey)
            {
                int id = data.Key.ToType<int>();
                if (id <= 0)
                {
                    id = GetNextId();
                    data.Key = id.ToType<TKey>();
                }
            }
            //加入缓存
            m_cacheStruct.AddOrUpdate(data.Key, data, (key, oldValue) => oldValue = data);

            //载入redis
            if (m_redisDb != null)
                m_redisDb.HashSet(EntityTypeName, data.Key.ToRedisValue(), data.ToRedisValue());

            //存入数据库
            SaveDbAsync(data, EntityActionEnum.Add);
        }

        public bool Update(TValue data)
        {
            CheckReady();
            if (TryGetValue(data.Key, out TValue value))
            {
                data.UpdateTime = DateTime.Now;
                SaveDbAsync(data, EntityActionEnum.Update);
                return true;
            }
            return false;
        }

        public bool Remove(TKey key)
        {
            CheckReady();
            if (TryGetValue(key, out TValue data))
            {
                SaveDbAsync(data, EntityActionEnum.Remove);

                m_cacheStruct.TryRemove(key, out data);
                if (m_redisDb != null)
                    m_redisDb.HashDelete(EntityTypeName, key.ToRedisValue());
                return true;
            }
            return false;
        }

        private void SaveDbAsync(TValue data, EntityActionEnum action)
        {
            if (action != EntityActionEnum.Update)
                data.Action = action;
            if (m_updateDb.TryAdd(data.Key, data))
            {
                data.Action = action;
            }
        }

        private async Task TrySaveDb()
        {
            while (true)
            {
                if (m_updateDb.Count >= MinToDbCount
                    || (m_updateDb.Count > 0 && MathUtils.DiffDate(m_lastSaveDbTime).TotalMilliseconds >= MillisecSaveInterval)
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

        public TValue Refresh(TKey key)
        {
            CheckReady();
            m_cacheStruct.TryRemove(key, out TValue data);
            if (m_redisDb != null)
                m_redisDb.HashDelete(EntityTypeName, key.ToRedisValue());
            TryGetValue(key, out data);
            return data;
        }

        public void Clear()
        {
            m_cacheStruct.Clear();
            if (m_redisDb != null)
                Keys.ForEach((key) => { m_redisDb.HashDelete(EntityTypeName, key.ToRedisValue()); });
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out TValue data);
        }

        public List<TValue> WhereToList(Func<TValue, bool> predicate)
        {
            List<TValue> datalist = null;
            if (DataAttribs.LoadInCache)
            {
                lock (m_lock_data)
                {
                    datalist = new List<TValue>();
                    Values.ForEach((data) =>
                    {
                        if (predicate(data))
                            datalist.Add(data);
                    });
                }
            }
            else
            {
                datalist = m_context.Set<TValue>().Where(predicate).ToList();
            }
            return datalist;
        }
        public TValue FirstOrDefault(Func<TValue, bool> predicate)
        {
            if (DataAttribs.LoadInCache)
            {
                return Values.FirstOrDefault((item) => predicate(item));
            }
            else
            {
                return m_context.Set<TValue>().FirstOrDefault(predicate);
            }
        }
        public bool Any(Func<TValue, bool> predicate)
        {
            if (DataAttribs.LoadInCache)
            {
                return Values.Any((item) => predicate(item));
            }
            else
            {
                return m_context.Set<TValue>().Any(predicate);
            }
        }

    }
}
