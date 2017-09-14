using System;
using System.Collections.Generic;

namespace CommonLibs
{
    public class SizeLimitedCache<TKey, TValue> : Cache<TKey, TValue>
    {
        private int _MaxSize;
        public int MaxSize
        {
            get { return _MaxSize; }
            set { _MaxSize = value; }
        }

        public SizeLimitedCache(int maxSize)
        {
            _MaxSize = maxSize;
        }

        protected override void UpdateElementAccess(TKey key, CacheValue<TKey, TValue> cacheValue)
        {
            base.UpdateElementAccess(key, cacheValue);
            while (_IndexList.Count > _MaxSize)
            {
                InvalidateUnlocked(_IndexList.Last.Value.Key);
            }
        }

        public virtual void Expire()
        {
            base.Expire(MaxSize);
        }
    }
}