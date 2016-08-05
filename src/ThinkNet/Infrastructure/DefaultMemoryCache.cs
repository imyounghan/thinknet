using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;

namespace ThinkNet.Infrastructure
{
    public class DefaultMemoryCache : ICache
    {
        private readonly BinaryFormatter _serializer;
        private readonly bool _enabled;
        private readonly MemoryCache cache;
        private readonly CacheItemPolicy policy;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public DefaultMemoryCache()
        {
            this._serializer = new BinaryFormatter();
            this._enabled = ConfigurationManager.AppSettings["thinkcfg.caching_enabled"].Change(false);

            this.cache = System.Runtime.Caching.MemoryCache.Default;
            this.policy = new CacheItemPolicy() {
                 SlidingExpiration = TimeSpan.FromSeconds(ConfigurationManager.AppSettings["thinkcfg.caching_expired"].Change(300))
            };
        }

        private byte[] Serialize(object obj)
        {
            using (var stream = new MemoryStream()) {
                _serializer.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        private object Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data)) {
                return _serializer.Deserialize(stream);
            }
        }

        /// <summary>
        /// 从缓存中获取该类型的实例。
        /// </summary>
        public object Get(Type type, object key)
        {
            if (!_enabled)
                return null;

            type.NotNull("type");
            key.NotNull("key");

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            object data = cache.Get(cacheKey, cacheRegion);
            if (data == null)
                return null;

            var de = (DictionaryEntry)data;
            if (key.ToString() == de.Key.ToString()) {
                return this.Deserialize((byte[])de.Value);
            }
            else {
                return null;
            }            
        }
        /// <summary>
        /// 设置实例到缓存
        /// </summary>
        public void Set(object entity, object key)
        {
            if (!_enabled)
                return;

            entity.NotNull("entity");
            key.NotNull("key");

            var type = entity.GetType();

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            var data = new DictionaryEntry(key, this.Serialize(entity));
            if (cache.Contains(cacheKey, cacheRegion)) {
                cache.Set(cacheKey, data, policy, cacheRegion);
            }
            else {
                cache.Add(cacheKey, data, policy, cacheRegion);
            }
        }
        /// <summary>
        /// 从缓存中移除
        /// </summary>
        public void Remove(Type type, object key)
        {
            if (!_enabled)
                return;

            type.NotNull("type");
            key.NotNull("key");

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            if (cache.Contains(cacheKey, cacheRegion)) {
                cache.Remove(cacheKey, cacheRegion);
            }
        }


        private static string GetCacheRegion(Type type)
        {
            var attr = type.GetAttribute<CacheRegionAttribute>(false);
            if (attr == null)
                return CacheRegionAttribute.DefaultRegionName;

            return attr.CacheRegion;
        }
        private static string BuildCacheKey(Type type, object key)
        {
            return string.Format("Entity:{0}:{1}", type.FullName, key);
        }
    }
}
