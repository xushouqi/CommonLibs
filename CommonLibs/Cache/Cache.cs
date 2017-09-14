using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLibs
{
    public class Cache<TKey, TValue> : IDisposable
    {
        protected class CacheValue<TCacheKey, TCacheValue>
        {
            public CacheValue(TKey key, TCacheValue value)
            {
                LastAccess = DateTime.Now;
                Value = value;
                Key = key;
            }

            public LinkedListNode<KeyValuePair<TCacheKey, CacheValue<TCacheKey, TCacheValue>>> IndexRef { get; set; }
            public DateTime LastAccess { get; set; }
            public TCacheValue Value { get; set; }
            public TKey Key { get; set; }
        }

        protected readonly LinkedList<KeyValuePair<TKey, CacheValue<TKey, TValue>>> _IndexList = new LinkedList<KeyValuePair<TKey, CacheValue<TKey, TValue>>>();
        private readonly Dictionary<TKey, CacheValue<TKey, TValue>> _ValueCache = new Dictionary<TKey, CacheValue<TKey, TValue>>();
        protected object SyncRoot = new object();
        private DateTime _LastCacheAccess = DateTime.MaxValue;

        public virtual int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _ValueCache.Count;
                }
            }
        }

        public TValue GetValue(TKey key)
        {
            TValue value = default(TValue);
            TryGetValue(key, out value);
            return value;
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            CacheValue<TKey, TValue> v;
            value = default(TValue);

            lock (SyncRoot)
            {
                _LastCacheAccess = DateTime.Now;
                v = GetCacheValueUnlocked(key);
                if (v != null)
                {
                    value = v.Value;
                    UpdateElementAccess(key, v);
                    return true;
                }
            }

            return false;
        }

        protected virtual void UpdateElementAccess(TKey key, CacheValue<TKey, TValue> cacheValue)
        {
            // update last access and move it to the head of the list
            cacheValue.LastAccess = DateTime.Now;
            var idxRef = cacheValue.IndexRef;
            if (idxRef != null)
            {
                _IndexList.Remove(idxRef);
            }
            else
            {
                idxRef = new LinkedListNode<KeyValuePair<TKey, CacheValue<TKey, TValue>>>(new KeyValuePair<TKey, CacheValue<TKey, TValue>>(key, cacheValue));
                cacheValue.IndexRef = idxRef;
            }
            _IndexList.AddFirst(idxRef);
        }

        protected virtual CacheValue<TKey, TValue> GetCacheValueUnlocked(TKey key)
        {
            CacheValue<TKey, TValue> v;
            return _ValueCache.TryGetValue(key, out v) ? v : null;
        }

        public virtual void SetValue(TKey key, TValue value)
        {
            lock (SyncRoot)
            {
                SetValueUnlocked(key, value);
            }
        }

        protected virtual CacheValue<TKey, TValue> SetValueUnlocked(TKey key, TValue value)
        {
            _LastCacheAccess = DateTime.Now;
            CacheValue<TKey, TValue> cacheValue = GetCacheValueUnlocked(key);
            if (cacheValue == null)
            {
                cacheValue = new CacheValue<TKey, TValue>(key, value);
                _ValueCache[key] = cacheValue;
            }
            else
            {
                cacheValue.Value = value;
            }
            UpdateElementAccess(key, cacheValue);
            return cacheValue;
        }

        public virtual void Invalidate(TKey key)
        {
            lock (SyncRoot)
            {
                _LastCacheAccess = DateTime.Now;
                InvalidateUnlocked(key);
            }
        }

        protected void InvalidateUnlocked(TKey key)
        {
            var value = GetCacheValueUnlocked(key);
            InvalidateUnlocked(value);
        }
        protected void InvalidateUnlocked(CacheValue<TKey, TValue> value)
        {
            if (value != null)
            {
                _ValueCache.Remove(value.Key);
                _IndexList.Remove(value.IndexRef);
            }
        }

        public virtual void Expire(TimeSpan maxAge)
        {
            lock (SyncRoot)
            {
                //var toExpire = _ValueCache.Where(x => IsExpired(x.Key, x.Value.Value, x.Value.LastAccess, maxAge)).Select(x => x.Key).ToList();
                //toExpire.ForEach(InvalidateUnlocked);
                //var toExpire = _ValueCache.Where(x => IsExpired(x.Value, maxAge)).Select(x => x.Value).ToList();
                //toExpire.ForEach(InvalidateUnlocked);

                while(_IndexList.Count > 0 && IsExpired(_IndexList.Last.Value.Value, maxAge))
                {
                    InvalidateUnlocked(_IndexList.Last.Value.Value);
                }
            }
        }

        protected virtual bool IsExpired(CacheValue<TKey, TValue> value, TimeSpan maxAge)
        {
            return value.LastAccess + maxAge < _LastCacheAccess;
        }
        protected virtual bool IsExpired(TKey key, TValue value, DateTime lastValueAccess, TimeSpan maxAge)
        {
            return lastValueAccess + maxAge < _LastCacheAccess;
        }

        public virtual void Expire(int maxSize)
        {
            lock (SyncRoot)
            {
                while (_IndexList.Count > maxSize)
                {
                    InvalidateUnlocked(_IndexList.Last.Value.Value);
                }
            }
        }

        public List<TKey> GetKeys()
        {
            lock (SyncRoot)
            {
                return new List<TKey>(_ValueCache.Keys);
            }
        }

        public virtual void Flush()
        {
            lock (SyncRoot)
            {
                _ValueCache.Clear();
                _IndexList.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
