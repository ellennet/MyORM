using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;

namespace MyORM.MyCache
{
    internal interface ICaching
    {
        void Add(string key, object value, bool isOverwrite = true);
        object Get(string key);
        void Remove(string key);
        bool IsCache(string key);
    }

    internal class Caching : ICaching
    {
        ObjectCache objectCache;

        public Caching()
        {
            objectCache = MemoryCache.Default;
        }

        /// <summary>
        /// 添加到缓存中
        /// </summary>
        /// <param name="key">KEY</param>
        /// <param name="value">Value</param>
        /// <param name="isOverwrite">是否覆盖相同KEY的值</param>
        public void Add(string key, object value, bool isOverwrite = true)
        {
            if (!isOverwrite)
            {
                if (IsCache(key))
                    return;
            }
            else
            {
                if (IsCache(key))
                    Remove(key);

                objectCache.Add(key, value, new DateTimeOffset(DateTime.Now.AddDays(14))); //7天过期
            }
        }

        /// <summary>
        /// 从缓存中读取
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public object Get(string key)
        {            
            return objectCache.Get(key);
        }

        /// <summary>
        /// 从缓存中移除
        /// </summary>
        /// <param name="key">KEY</param>
        public void Remove(string key)
        {
            objectCache.Remove(key);
        }

        /// <summary>
        /// 是否有缓存
        /// </summary>
        /// <param name="key">KEY</param>
        /// <returns></returns>
        public bool IsCache(string key)
        {
            return objectCache.Contains(key);
        }
    }
}
