using System;
using System.Collections.Generic;
using System.Text;
using CommonLibs;

namespace CommonServices
{
    /// <summary>
    /// 缓存项类型
    /// </summary>
    public enum CacheType
    {
        /// <summary>
        /// 空
        /// </summary>
        None,
        /// <summary>
        /// 单一实体
        /// </summary>
        Entity,
        /// <summary>
        /// 
        /// </summary>
        Dictionary,
        /// <summary>
        /// List of rank.
        /// </summary>
        Rank,
        /// <summary>
        /// 队列方式
        /// </summary>
        Queue
    }

    public class CacheItem
    {
        public CacheType CacheType;

        private object _itemData;
        private DateTime _lastAccessTime;
        private int _lifeTime = 0;

        public void Set(object data)
        {
            _itemData = data;
            _lastAccessTime = DateTime.Now;
        }

        public object Get()
        {
            _lastAccessTime = DateTime.Now;
            return _itemData;
        }

        /// <summary>
        /// 缓存是否已过期
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return _lifeTime > 0 && MathUtils.DiffDate(_lastAccessTime).TotalSeconds > _lifeTime;
            }
        }
    }
}
