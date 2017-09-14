using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CommonLibs;
using DotNetCore.CAP;

namespace CommonServices.Caching
{
    //public interface IHybridCacheClient : ICacheClient { }

    public class HybridCacheClient<T> : ICacheClient<T> where T : Entity
    {
        //protected readonly ICacheClient<T> _distributedCache;
        private readonly InMemoryCacheClient<T> _localCache;
        //protected readonly IMessageBus _messageBus;
        private readonly ICapPublisher _publisher;
        private readonly ILogger _logger;
        private long _localCacheHits;
        private long _invalidateCacheCalls;
        private readonly IServiceProvider _services;

        //private readonly string _cacheId = Guid.NewGuid().ToString("N");
        private readonly string _cacheId = string.Empty;
        private readonly string m_removeKey = string.Empty;

        public HybridCacheClient(ICapPublisher publisher,
            IServiceProvider services,
            ILoggerFactory loggerFactory = null
            //, ICacheClient<T> distributedCacheClient
            )
        {
            _services = services;
            _logger = loggerFactory.CreateLogger<HybridCacheClient<T>>();
            //_distributedCache = distributedCacheClient;
            _publisher = publisher;
            //_messageBus.SubscribeAsync<InvalidateCache>(OnRemoteCacheItemExpiredAsync).GetAwaiter().GetResult();
            _localCache = new InMemoryCacheClient<T>(new InMemoryCacheClientOptions { LoggerFactory = loggerFactory }) { MaxItems = 100 };
            _localCache.ItemExpired.AddHandler(OnLocalCacheItemExpiredAsync);

            _cacheId = typeof(T).Name;
            m_removeKey = "cache.data.remove";
            //m_removeKey = "cache.data.remove." + _cacheId;
        }

        private IRepository<T> _repository
        {
            get
            {
                return (IRepository<T>)_services.GetService(typeof(IRepository<T>));
            }
        }

        public InMemoryCacheClient<T> LocalCache => _localCache;
        public long LocalCacheHits => _localCacheHits;
        public long InvalidateCacheCalls => _invalidateCacheCalls;

        public int LocalCacheSize
        {
            get { return _localCache.MaxItems ?? -1; }
            set { _localCache.MaxItems = value; }
        }

        private async Task OnLocalCacheItemExpiredAsync(object sender, EntityExpiredEventArgs<T> args)
        {
            if (!args.SendNotification)
                return;

            _logger.LogTrace("Local cache expired event: key={0}", args.Key);
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { args.Key }, Expired = true });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { args.Key }, Expired = true }).AnyContext();
        }

        public async Task OnRemoteCacheItemExpiredAsync(InvalidateCache message)
        {
            if (!String.IsNullOrEmpty(message.CacheId) && String.Equals(_cacheId, message.CacheId))
                return;

            _logger.LogTrace(String.Format("Invalidating local cache from remote: id={0} expired={1} keys={2}", message.CacheId, message.Expired, String.Join(",", message.Keys ?? new string[] { })));
            Interlocked.Increment(ref _invalidateCacheCalls);
            if (message.FlushAll)
            {
                await _localCache.RemoveAllAsync().AnyContext();
                _logger.LogTrace("Fushed local cache");
            }
            else if (message.Keys != null && message.Keys.Length > 0)
            {
                var keysToRemove = new List<string>(message.Keys.Length);
                foreach (string key in message.Keys)
                {
                    if (message.Expired)
                        await _localCache.RemoveExpiredKeyAsync(key, false).AnyContext();
                    else if (key.EndsWith("*"))
                        await _localCache.RemoveByPrefixAsync(key.Substring(0, key.Length - 1)).AnyContext();
                    else
                        keysToRemove.Add(key);
                }

                int results = await _localCache.RemoveAllAsync(keysToRemove).AnyContext();
                _logger.LogTrace("Removed {0} keys from local cache", results);
            }
            else
            {
                _logger.LogWarning("Unknown invalidate cache message");
            }
        }

        public async Task<int> RemoveAllAsync(IEnumerable<string> keys = null)
        {
            bool flushAll = keys == null || !keys.Any();
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, FlushAll = flushAll, Keys = keys?.ToArray() });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, FlushAll = flushAll, Keys = keys?.ToArray() }).AnyContext();
            return await _localCache.RemoveAllAsync(keys).AnyContext();
            //return await _distributedCache.RemoveAllAsync(keys).AnyContext();
        }

        public async Task<int> RemoveByPrefixAsync(string prefix)
        {
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { prefix + "*" } });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { prefix + "*" } }).AnyContext();
            return await _localCache.RemoveByPrefixAsync(prefix).AnyContext();
            //return await _distributedCache.RemoveByPrefixAsync(prefix).AnyContext();
        }

        public async Task<T> GetAsync(string prefix, int id)
        {
            var key = string.Concat(prefix, "_", id);
            var cacheValue = await _localCache.GetAsync(key).AnyContext();
            if (cacheValue != null)
            {
                _logger.LogTrace("Local cache hit: {0}", key);
                Interlocked.Increment(ref _localCacheHits);
            }
            else
            {
                _logger.LogTrace("Local cache miss: {0}", key);
                if (prefix.Equals("ID"))
                {
                    using (var repo = _repository)
                    {
                        cacheValue = repo.Find(id);
                    }
                    await _localCache.SetAsync(key, cacheValue);
                }
            }
            return cacheValue;
        }
        public async Task<T> GetAsync(int id)
        {
            return await GetAsync("ID", id);
        }
        public async Task<T> GetAsync(string key)
        {
            var cacheValue = await _localCache.GetAsync(key).AnyContext();
            if (cacheValue != null)
            {
                _logger.LogTrace("Local cache hit: {0}", key);
                Interlocked.Increment(ref _localCacheHits);
            }
            else
                _logger.LogTrace("Local cache miss: {0}", key);
            return cacheValue;

            //cacheValue = await _distributedCache.GetAsync(key).AnyContext();
            //if (cacheValue.HasValue)
            //{
            //    var expiration = await _distributedCache.GetExpirationAsync(key).AnyContext();
            //    _logger.LogTrace("Setting Local cache key: {0} with expiration: {1}", key, expiration);

            //    await _localCache.SetAsync(key, cacheValue.Value, expiration).AnyContext();
            //    return cacheValue;
            //}

            //return cacheValue.HasValue ? cacheValue : CacheValue<T>.NoValue;
        }

        public async Task<IDictionary<int, T>> GetAllAsync(IEnumerable<int> ids)
        {
            var valueMap = new Dictionary<int, T>();
            foreach (var id in ids)
            {
                var data = await GetAsync(id);
                if (data != null)
                    valueMap[id] = data;
            }
            return valueMap;
            //return _distributedCache.GetAllAsync(keys);
        }
        public async Task<IDictionary<string, T>> GetAllAsync(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var data = await GetAsync(key);
                if (data != null)
                    valueMap[key] = data;
            }
            return valueMap;
            //return _localCache.GetAllAsync(keys);
            //return _distributedCache.GetAllAsync(keys);
        }
        public async Task<ICollection<T>> GetCollectionAsync(IEnumerable<int> ids)
        {
            var valueMap = new List<T>();
            foreach (var key in ids)
            {
                var data = await GetAsync(key);
                if (data != null)
                    valueMap.Add(data);
            }
            return valueMap;
        }

        public async Task<bool> AddAsync(int id, T value, TimeSpan? expiresIn = null)
        {
            return await AddAsync("ID", value, expiresIn);
        }
        public async Task<bool> AddAsync(string prefix, int id, T value, TimeSpan? expiresIn = null)
        {
            var key = string.Concat(prefix, "_", id);
            return await AddAsync(key, value, expiresIn);
        }
        public async Task<bool> AddAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            _logger.LogTrace("Adding key \"{0}\" to local cache with expiration: {1}", key, expiresIn);
            //bool added = await _distributedCache.AddAsync(key, value, expiresIn).AnyContext();
            //if (added)
                bool added = await _localCache.SetAsync(key, value, expiresIn).AnyContext();

            return added;
        }

        public async Task<bool> SetAsync(int id, T value, TimeSpan? expiresIn = null)
        {
            return await SetAsync("ID", id, value, expiresIn);
        }
        public async Task<bool> SetAsync(string prefix, int id, T value, TimeSpan? expiresIn = null)
        {
            var key = string.Concat(prefix, "_", id);
            return await SetAsync(key, value, expiresIn);
        }
        public async Task<bool> SetAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            _logger.LogTrace("Setting key \"{0}\" to local cache with expiration: {1}", key, expiresIn);
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } }).AnyContext();
            return await _localCache.SetAsync(key, value, expiresIn).AnyContext();

            //return await _distributedCache.SetAsync(key, value, expiresIn).AnyContext();
        }

        public async Task<int> SetAllAsync(IDictionary<string, T> values, TimeSpan? expiresIn = null)
        {
            if (values == null || values.Count == 0)
                return 0;

            _logger.LogTrace("Adding keys \"{0}\" to local cache with expiration: {1}", values.Keys, expiresIn);
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = values.Keys.ToArray() });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = values.Keys.ToArray() }).AnyContext();
            return await _localCache.SetAllAsync(values, expiresIn).AnyContext();
            //return await _distributedCache.SetAllAsync(values, expiresIn).AnyContext();
        }

        public async Task<bool> ReplaceAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } }).AnyContext();
            return await _localCache.ReplaceAsync(key, value, expiresIn).AnyContext();
            //return await _distributedCache.ReplaceAsync(key, value, expiresIn).AnyContext();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await ExistsAsync("ID", id);
        }
        public async Task<bool> ExistsAsync(string prefix, int id)
        {
            var key = string.Concat(prefix, "ID", id);
            bool ret = await _localCache.ExistsAsync(key);
            if (!ret)
            {
                using (var repo = _repository)
                {
                    var data = repo.Find(id);
                    if (data != null)
                    {
                        ret = true;
                        await _localCache.SetAsync(key, data);
                    }
                }
            }
            return ret;
        }
        public async Task<bool> ExistsAsync(string key)
        {
            bool ret = await _localCache.ExistsAsync(key);
            if (!ret)
            {
                using (var repo = _repository)
                {
                    ret = repo.Find(key) != null;
                }
            }
            return ret;
            //return _distributedCache.ExistsAsync(key);
        }

        public Task<TimeSpan?> GetExpirationAsync(string key)
        {
            return _localCache.GetExpirationAsync(key);
            //return _distributedCache.GetExpirationAsync(key);
        }

        public async Task SetExpirationAsync(string key, TimeSpan expiresIn)
        {
            await _localCache.SetExpirationAsync(key, expiresIn).AnyContext();
            //await _distributedCache.SetExpirationAsync(key, expiresIn).AnyContext();
            await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } });
            //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } }).AnyContext();
        }

#region DirectReposity
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _repository.AnyAsync(predicate);
        }
        public async Task<T> MaxAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return await _repository.MaxAsync(where, order);
        }
        public async Task<T> MinAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return await _repository.MinAsync(where, order);
        }
        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken token = default(CancellationToken))
        {
            return await _repository.FirstOrDefaultAsync(predicate, token);
        }
        public IQueryable<T> Where<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return _repository.Where(where, order);
        }
        public IQueryable<T> Where(Expression<Func<T, bool>> where)
        {
            return _repository.Where(where);
        }
        public async Task<T[]> WhereToArrayAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return await _repository.WhereToArrayAsync(where, order);
        }
        public async Task<T[]> WhereToArrayAsync(Expression<Func<T, bool>> where)
        {
            return await _repository.WhereToArrayAsync(where);
        }
        public async Task<List<T>> WhereToListAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return await _repository.WhereToListAsync(where, order);
        }
        public async Task<List<T>> WhereToListAsync(Expression<Func<T, bool>> where)
        {
            return await _repository.WhereToListAsync(where);
        }

        public async Task AddAsync(T item)
        {
            using (var repo = _repository)
            {
                repo.Add(item);
                await repo.SaveChangesAsync();

                await AddAsync(item.GetId(), item);
            }
        }
        public async Task UpdateAsync(T item)
        {
            using (var repo = _repository)
            {
                repo.Update(item);
                await repo.SaveChangesAsync();

                await SetAsync(item.GetId(), item);
            }
        }
        public async Task RemoveAsync(T item)
        {
            var key = string.Concat("ID", item.GetId());
            await RemoveAllAsync(new List<string>() { key });

            using (var repo = _repository)
            {
                repo.Remove(item);
                await repo.SaveChangesAsync();
            }
        }
        public async Task AddRangeAsync(IEnumerable<T> datas)
        {
            using (var repo = _repository)
            {
                foreach (var item in datas)
                {
                    repo.Add(item);

                    await AddAsync(item.GetId(), item);
                }
                await repo.SaveChangesAsync();
            }
        }
        public async Task UpdateRangeAsync(IEnumerable<T> datas)
        {
            using (var repo = _repository)
            {
                foreach (var item in datas)
                {
                    repo.Update(item);

                    await SetAsync(item.GetId(), item);
                }
                await repo.SaveChangesAsync();
            }
        }
        public async Task RemoveRangeAsync(IEnumerable<T> datas)
        {
            List<string> keys = new List<string>();
            foreach (var item in datas)
            {
                keys.Add(string.Concat("ID", item.GetId()));
            }
            await RemoveAllAsync(keys);

            using (var repo = _repository)
            {
                repo.RemoveRange(datas.ToArray());
                await repo.SaveChangesAsync();
            }
        }
        public async Task<int> RemoveWhereAsync(Expression<Func<T, bool>> predicate)
        {
            await RemoveAllAsync();
            int ret = 0;
            using (var repo = _repository)
            {
                ret = await repo.RemoveWhereAsync(predicate);
                await repo.SaveChangesAsync();
            }
            return ret;
        }
        #endregion

        #region collectsets
        //public async Task<long> SetAddAsync(string key, IEnumerable<T> values, TimeSpan? expiresIn = null)
        //{
        //    await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } });
        //    //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } }).AnyContext();
        //    await _localCache.SetAddAsync(key, values, expiresIn).AnyContext();
        //    return await _distributedCache.SetAddAsync(key, values, expiresIn).AnyContext();
        //}

        //public async Task<long> SetRemoveAsync(string key, IEnumerable<T> values, TimeSpan? expiresIn = null)
        //{
        //    await _publisher.PublishAsync(m_removeKey, new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } });
        //    //await _messageBus.PublishAsync(new InvalidateCache { CacheId = _cacheId, Keys = new[] { key } }).AnyContext();
        //    await _localCache.SetRemoveAsync(key, values, expiresIn).AnyContext();
        //    return await _distributedCache.SetRemoveAsync(key, values, expiresIn).AnyContext();
        //}

        //public async Task<ICollection<T>> GetSetAsync(string key)
        //{
        //    var cacheValue = await _localCache.GetSetAsync(key).AnyContext();
        //    if (cacheValue.HasValue)
        //    {
        //        _logger.LogTrace("Local cache hit: {0}", key);
        //        Interlocked.Increment(ref _localCacheHits);
        //        return cacheValue;
        //    }

        //    _logger.LogTrace("Local cache miss: {0}", key);
        //    cacheValue = await _distributedCache.GetSetAsync(key).AnyContext();
        //    if (cacheValue.HasValue)
        //    {
        //        var expiration = await _distributedCache.GetExpirationAsync(key).AnyContext();
        //        _logger.LogTrace("Setting Local cache key: {0} with expiration: {1}", key, expiration);

        //        await _localCache.SetAddAsync(key, cacheValue.Value, expiration).AnyContext();
        //        return cacheValue;
        //    }

        //    return cacheValue.HasValue ? cacheValue : null;
        //}
        #endregion

        public virtual void Dispose()
        {
            _localCache.ItemExpired.RemoveHandler(OnLocalCacheItemExpiredAsync);
            _localCache.Dispose();

            // TODO: unsubscribe handler from messagebus.
        }
    }
}
