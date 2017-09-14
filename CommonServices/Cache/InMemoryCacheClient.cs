using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CommonServices.Utility;
using CommonLibs;

namespace CommonServices.Caching
{
    public class InMemoryCacheClient<T> : MaintenanceBase, ICacheClient<T> where T : Entity
    {
        private readonly ConcurrentDictionary<string, CacheEntry<T>> _memory;
        //private readonly ConcurrentDictionary<string, CacheEntry<ICollection<T>>> _memorySets;
        private long _hits;
        private long _misses;

        public InMemoryCacheClient(InMemoryCacheClientOptions options) : base(options.LoggerFactory)
        {
            ShouldCloneValues = true;
            _memory = new ConcurrentDictionary<string, CacheEntry<T>>();
            //_memorySets = new ConcurrentDictionary<string, CacheEntry<ICollection<T>>>();
            InitializeMaintenance();
        }

        public int Count => _memory.Count;
        public int? MaxItems { get; set; }
        public bool ShouldCloneValues { get; set; }
        public long Hits => _hits;
        public long Misses => _misses;

        public AsyncEvent<EntityExpiredEventArgs<T>> ItemExpired { get; } = new AsyncEvent<EntityExpiredEventArgs<T>>();

        private Task OnItemExpiredAsync(string key, bool sendNotification = true)
        {
            var args = new EntityExpiredEventArgs<T>
            {
                Client = this,
                Key = key,
                SendNotification = sendNotification
            };

            return ItemExpired?.InvokeAsync(this, args) ?? Task.CompletedTask;
        }

        public ICollection<string> Keys
        {
            get
            {
                return _memory.ToArray()
                        .OrderBy(kvp => kvp.Value.LastAccessTicks)
                        .ThenBy(kvp => kvp.Value.InstanceNumber)
                        .Select(kvp => kvp.Key)
                        .ToList();
            }
        }

        public Task<int> RemoveAllAsync(IEnumerable<string> keys = null)
        {
            if (keys == null)
            {
                var count = _memory.Count;
                _memory.Clear();
                return Task.FromResult(count);
            }

            int removed = 0;
            foreach (var key in keys)
            {
                if (String.IsNullOrEmpty(key))
                    continue;

                _logger.LogTrace("RemoveAllAsync: Removing key {0}", key);
                CacheEntry<T> item;
                if (_memory.TryRemove(key, out item))
                    removed++;
            }

            return Task.FromResult(removed);
        }

        public Task<int> RemoveByPrefixAsync(string prefix)
        {
            var keysToRemove = new List<string>();
            var regex = new Regex(String.Concat(prefix, "*").Replace("*", ".*").Replace("?", ".+"));
            try
            {
                foreach (var key in _memory.Keys.ToList())
                    if (regex.IsMatch(key))
                        keysToRemove.Add(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to remove items from cache with this {0} prefix", prefix);
            }

            return RemoveAllAsync(keysToRemove);
        }

        internal Task RemoveExpiredKeyAsync(string key, bool sendNotification = true)
        {
            _logger.LogTrace("Removing expired key {0}", key);

            CacheEntry<T> cacheEntry;
            if (_memory.TryRemove(key, out cacheEntry))
                return OnItemExpiredAsync(key, sendNotification);

            return Task.CompletedTask;
        }

        public async Task<T> GetAsync(int id)
        {
            return await GetAsync("ID", id);
        }
        public async Task<T> GetAsync(string prefix, int id)
        {
            string key = string.Concat(prefix, "_", id);
            return await GetAsync(key);
        }
        public async Task<T> GetAsync(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            CacheEntry<T> cacheEntry;
            if (!_memory.TryGetValue(key, out cacheEntry))
            {
                Interlocked.Increment(ref _misses);
                return default(T);
            }

            if (cacheEntry.ExpiresAt < SystemClock.UtcNow)
            {
                await RemoveExpiredKeyAsync(key);
                Interlocked.Increment(ref _misses);
                return default(T);
            }

            Interlocked.Increment(ref _hits);

            try
            {
                T value = cacheEntry.GetValue();
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to deserialize value \"{0}\" to type {1}", cacheEntry.Value, typeof(T).FullName);
                return default(T);
            }
        }

        public async Task<IDictionary<int, T>> GetAllAsync(IEnumerable<int> ids)
        {
            var valueMap = new Dictionary<int, T>();
            foreach (var id in ids)
                valueMap[id] = await GetAsync(id.ToString());

            return valueMap;
        }
        public async Task<IDictionary<string, T>> GetAllAsync(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
                valueMap[key] = await GetAsync(key);

            return valueMap;
        }
        public async Task<ICollection<T>> GetCollectionAsync(IEnumerable<int> ids)
        {
            var valueMap = new List<T>();
            foreach (var id in ids)
            {
                var data = await GetAsync(id.ToString());
                if (data != null)
                    valueMap.Add(data);
            }
            return valueMap;
        }

        public Task<bool> AddAsync(int id, T value, TimeSpan? expiresIn = null)
        {
            return AddAsync("ID", id, value, expiresIn);
        }
        public Task<bool> AddAsync(string prefix, int id, T value, TimeSpan? expiresIn = null)
        {
            var key = string.Concat(prefix, "_", id);
            return AddAsync(key, value, expiresIn);
        }
        public Task<bool> AddAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            DateTime expiresAt = expiresIn.HasValue ? SystemClock.UtcNow.Add(expiresIn.Value) : DateTime.MaxValue;
            return SetInternalAsync(key, new CacheEntry<T>(value, expiresAt), true);
        }

        public Task<bool> SetAsync(int id, T value, TimeSpan? expiresIn = null)
        {
            return SetAsync("ID", id, value, expiresIn);
        }
        public Task<bool> SetAsync(string prefix, int id, T value, TimeSpan? expiresIn = null)
        {
            var key = string.Concat(prefix, "_", id);
            return SetAsync(key, value, expiresIn);
        }
        public Task<bool> SetAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            // TODO: Look up the existing expiration if expiresIn is null.
            DateTime expiresAt = expiresIn.HasValue ? SystemClock.UtcNow.Add(expiresIn.Value) : DateTime.MaxValue;
            return SetInternalAsync(key, new CacheEntry<T>(value, expiresAt));
        }

        private async Task CleanupAsync()
        {
            if (!MaxItems.HasValue || _memory.Count <= MaxItems.Value)
                return;

            string oldest = _memory.ToArray()
                                   .OrderBy(kvp => kvp.Value.LastAccessTicks)
                                   .ThenBy(kvp => kvp.Value.InstanceNumber)
                                   .First()
                                   .Key;

            _logger.LogTrace("Removing key {oldest}", oldest);

            CacheEntry<T> cacheEntry;
            _memory.TryRemove(oldest, out cacheEntry);
            if (cacheEntry != null && cacheEntry.ExpiresAt < SystemClock.UtcNow)
                await OnItemExpiredAsync(oldest);
        }

        private async Task<bool> SetInternalAsync(string key, CacheEntry<T> entry, bool addOnly = false)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "SetInternalAsync: Key cannot be null or empty.");

            if (entry.ExpiresAt < SystemClock.UtcNow)
            {
                await RemoveExpiredKeyAsync(key);
                return false;
            }

            if (addOnly)
            {
                if (!_memory.TryAdd(key, entry))
                {
                    CacheEntry<T> existingEntry;
                    if (!_memory.TryGetValue(key, out existingEntry) || existingEntry.ExpiresAt >= SystemClock.UtcNow)
                        return false;

                    _memory.AddOrUpdate(key, entry, (k, cacheEntry) => entry);
                }

                _logger.LogTrace("Added cache key: {key}", key);
            }
            else
            {
                _memory.AddOrUpdate(key, entry, (k, cacheEntry) => entry);
                _logger.LogTrace("Set cache key: {0}", key);
            }

            ScheduleNextMaintenance(entry.ExpiresAt);
            await CleanupAsync();

            return true;
        }

        public async Task<int> SetAllAsync(IDictionary<string, T> values, TimeSpan? expiresIn = null)
        {
            if (values == null || values.Count == 0)
                return 0;

            var result = 0;
            foreach (var entry in values)
                if (await SetAsync(entry.Key, entry.Value))
                    result++;

            return result;
        }

        public Task<bool> ReplaceAsync(string key, T value, TimeSpan? expiresIn = null)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            if (!_memory.ContainsKey(key))
                return Task.FromResult(false);

            return SetAsync(key, value, expiresIn);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await ExistsAsync("ID", id);
        }
        public async Task<bool> ExistsAsync(string prefix, int id)
        {
            var key = string.Concat(prefix, "_", id);
            return await ExistsAsync(key);
        }
        public async Task<bool> ExistsAsync(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            return await Task.FromResult(_memory.ContainsKey(key));
        }

        public async Task<TimeSpan?> GetExpirationAsync(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            CacheEntry<T> value;
            if (!_memory.TryGetValue(key, out value) || value.ExpiresAt == DateTime.MaxValue)
                return null;

            if (value.ExpiresAt >= SystemClock.UtcNow)
                return value.ExpiresAt.Subtract(SystemClock.UtcNow);

            await RemoveExpiredKeyAsync(key);
            return null;
        }

        public async Task SetExpirationAsync(string key, TimeSpan expiresIn)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            DateTime expiresAt = SystemClock.UtcNow.Add(expiresIn);
            if (expiresAt < SystemClock.UtcNow)
            {
                await RemoveExpiredKeyAsync(key);
                return;
            }

            CacheEntry<T> value;
            if (_memory.TryGetValue(key, out value))
            {
                value.ExpiresAt = expiresAt;
                ScheduleNextMaintenance(expiresAt);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _memory.Clear();
            ItemExpired?.Dispose();
        }

        protected override async Task<DateTime?> DoMaintenanceAsync()
        {
            _logger.LogTrace("DoMaintenanceAsync");
            var expiredKeys = new List<string>();

            DateTime utcNow = SystemClock.UtcNow.AddMilliseconds(50);
            DateTime minExpiration = DateTime.MaxValue;

            try
            {
                foreach (var kvp in _memory)
                {
                    var expiresAt = kvp.Value.ExpiresAt;
                    if (expiresAt <= utcNow)
                        expiredKeys.Add(kvp.Key);
                    else if (expiresAt < minExpiration)
                        minExpiration = expiresAt;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to find expired cache items.");
            }

            foreach (var key in expiredKeys)
                await RemoveExpiredKeyAsync(key);

            return minExpiration;
        }

        #region DirectReposity
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            throw new NotImplementedException();
        }
        public async Task<T> MaxAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            throw new NotImplementedException();
        }
        public async Task<T> MinAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            throw new NotImplementedException();
        }
        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
        public IQueryable<T> Where<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            throw new NotImplementedException();
        }
        public IQueryable<T> Where(Expression<Func<T, bool>> where)
        {
            throw new NotImplementedException();
        }
        public async Task<T[]> WhereToArrayAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            throw new NotImplementedException();
        }
        public async Task<T[]> WhereToArrayAsync(Expression<Func<T, bool>> where)
        {
            return await WhereToArrayAsync(where);
        }
        public async Task<List<T>> WhereToListAsync<TKey>(Expression<Func<T, bool>> where, Expression<Func<T, TKey>> order)
        {
            return await WhereToListAsync(where, order);
        }
        public async Task<List<T>> WhereToListAsync(Expression<Func<T, bool>> where)
        {
            return await WhereToListAsync(where);
        }

        public async Task AddAsync(T item)
        {
            throw new NotImplementedException();
        }
        public async Task UpdateAsync(T item)
        {
            throw new NotImplementedException();
        }
        public async Task RemoveAsync(T item)
        {
            throw new NotImplementedException();
        }
        public async Task AddRangeAsync(IEnumerable<T> datas)
        {
            throw new NotImplementedException();
        }
        public async Task UpdateRangeAsync(IEnumerable<T> datas)
        {
            throw new NotImplementedException();
        }
        public async Task RemoveRangeAsync(IEnumerable<T> datas)
        {
            throw new NotImplementedException();
        }
        public async Task<int> RemoveWhereAsync(Expression<Func<T, bool>> predicate)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region CollectionSet

        //public async Task<long> SetAddAsync(string key, IEnumerable<T> values, TimeSpan? expiresIn = null)
        //{
        //    if (String.IsNullOrEmpty(key))
        //        throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        //    if (values == null)
        //        throw new ArgumentNullException(nameof(values));

        //    // TODO: Look up the existing expiration if expiresIn is null.
        //    DateTime expiresAt = expiresIn.HasValue ? SystemClock.UtcNow.Add(expiresIn.Value) : DateTime.MaxValue;
        //    if (expiresAt < SystemClock.UtcNow)
        //    {
        //        await RemoveExpiredKeyAsync(key);
        //        return default(long);
        //    }

        //    var items = new HashSet<T>(values);
        //    var entry = new CacheEntry<ICollection<T>>(items, expiresAt);
        //    _memory.AddOrUpdate(key, entry, (k, cacheEntry) =>
        //    {
        //        var collection = cacheEntry.Value as ICollection<T>;
        //        if (collection == null)
        //            throw new InvalidOperationException($"Unable to add value for key: {key}. Cache value does not contain a set.");

        //        collection.AddRange(items);
        //        cacheEntry.Value = collection;
        //        cacheEntry.ExpiresAt = expiresAt;
        //        return cacheEntry;
        //    });

        //    ScheduleNextMaintenance(expiresAt);
        //    await CleanupAsync();

        //    return items.Count;
        //}

        //public async Task<long> SetRemoveAsync(string key, IEnumerable<T> values, TimeSpan? expiresIn = null)
        //{
        //    if (String.IsNullOrEmpty(key))
        //        throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        //    if (values == null)
        //        throw new ArgumentNullException(nameof(values));

        //    DateTime expiresAt = expiresIn.HasValue ? SystemClock.UtcNow.Add(expiresIn.Value) : DateTime.MaxValue;
        //    if (expiresAt < SystemClock.UtcNow)
        //    {
        //        await RemoveExpiredKeyAsync(key);
        //        return default(long);
        //    }

        //    var items = new HashSet<T>(values);
        //    _memory.TryUpdate(key, (k, cacheEntry) =>
        //    {
        //        var collection = cacheEntry.Value as ICollection<T>;
        //        if (collection != null && collection.Count > 0)
        //        {
        //            foreach (var value in items)
        //            {
        //                if (collection.Contains(value))
        //                {
        //                    collection.Remove(value);
        //                }
        //            }

        //            cacheEntry.Value = collection;
        //        }

        //        cacheEntry.ExpiresAt = expiresAt;
        //        _logger.LogTrace("Removed value from set with cache key: {key}", key);
        //        return cacheEntry;
        //    });

        //    return items.Count;
        //}

        //public Task<ICollection<T>> GetSetAsync(string key)
        //{
        //    if (String.IsNullOrEmpty(key))
        //        throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        //    return GetAsync(key);
        //}

        #endregion

        private class CacheEntry<TEntity>
        {
            private TEntity _cacheValue;
            private static long _instanceCount;
            private readonly bool _shouldClone = false;
#if DEBUG
            private long _usageCount;
#endif

            public CacheEntry(TEntity value, DateTime expiresAt)
            {
                //_shouldClone = shouldClone && TypeRequiresCloning(value?.GetType());
                Value = value;
                ExpiresAt = expiresAt;
                LastModifiedTicks = SystemClock.UtcNow.Ticks;
                InstanceNumber = Interlocked.Increment(ref _instanceCount);
            }

            internal long InstanceNumber { get; private set; }
            internal DateTime ExpiresAt { get; set; }
            internal long LastAccessTicks { get; private set; }
            internal long LastModifiedTicks { get; private set; }
#if DEBUG
            internal long UsageCount => _usageCount;
#endif

            internal TEntity Value
            {
                get
                {
                    LastAccessTicks = SystemClock.UtcNow.Ticks;
#if DEBUG
                    Interlocked.Increment(ref _usageCount);
#endif
                    return _cacheValue;
                }
                set
                {
                    _cacheValue = value;
                    LastAccessTicks = SystemClock.UtcNow.Ticks;
                    LastModifiedTicks = SystemClock.UtcNow.Ticks;
                }
            }

            public TEntity GetValue()
            {
                return Value;
            }
        }

        public async Task OnRemoteCacheItemExpiredAsync(InvalidateCache message)
        {

        }
    }

    public class EntityExpiredEventArgs<T> : EventArgs where T : Entity
    {
        public InMemoryCacheClient<T> Client { get; set; }
        public string Key { get; set; }
        public bool SendNotification { get; set; }
    }
}