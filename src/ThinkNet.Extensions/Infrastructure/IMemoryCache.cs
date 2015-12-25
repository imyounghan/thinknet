using System;
using System.Configuration;
using System.Reflection;
using ThinkLib.Caching;
using ThinkNet.Annotation;
using ThinkNet.Infrastructure.Serialization;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 设置或获取聚合的缓存接口
    /// </summary>
    [RequiredComponent(typeof(MemoryCache))]
    public interface IMemoryCache
    {
        /// <summary>
        /// 从缓存获取聚合实例
        /// </summary>
        object Get(Type type, object key);
        /// <summary>
        /// 设置一个聚合实例入缓存。不存在加入缓存，存在更新缓存
        /// </summary>
        void Set(object entity, object key);
        /// <summary>
        /// 从缓存中移除聚合根
        /// </summary>
        void Remove(Type type, object key);
    }
    internal class MemoryCache : IMemoryCache
    {
        internal readonly static IMemoryCache Instance = new MemoryCache();
        private MemoryCache()
        { }

        private readonly IBinarySerializer _serializer;
        private readonly bool _enabled;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MemoryCache(IBinarySerializer serializer)
        {
            this._serializer = serializer;
            this._enabled = ConfigurationManager.AppSettings["thinkcfg.caching_enabled"].Safe("false").ToBoolean();
        }
        /// <summary>
        /// 从缓存中获取该类型的实例。
        /// </summary>
        public object Get(Type type, object key)
        {
            if (!_enabled)
                return null;

            Ensure.NotNull(type, "type");
            Ensure.NotNull(key, "key");


            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            object data = null;
            lock (cacheKey) {
                data = CacheManager.GetCache(cacheRegion).Get(cacheKey);
            }
            if (data == null)
                return null;

            return _serializer.Deserialize((byte[])data, type);
        }
        /// <summary>
        /// 设置实例到缓存
        /// </summary>
        public void Set(object entity, object key)
        {
            if (!_enabled)
                return;

            Ensure.NotNull(entity, "entity");
            Ensure.NotNull(key, "key");

            var type = entity.GetType();

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            var data = _serializer.Serialize(entity);

            lock (cacheKey) {
                CacheManager.GetCache(cacheRegion).Put(cacheKey, data);
            }
        }
        /// <summary>
        /// 从缓存中移除
        /// </summary>
        public void Remove(Type type, object key)
        {
            if (!_enabled)
                return;

            Ensure.NotNull(type, "type");
            Ensure.NotNull(key, "key");

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            lock (cacheKey) {
                CacheManager.GetCache(cacheRegion).Remove(cacheKey);
            }
        }


        private static string GetCacheRegion(Type type)
        {
            var attr = type.GetAttribute<CacheRegionAttribute>(false);
            if (attr == null)
                return CacheManager.CacheRegion;

            return attr.CacheRegion;
        }
        private static string BuildCacheKey(Type type, object key)
        {
            return string.Format("Entity:{0}:{1}", type.FullName, key.ToString());
        }
    }
}
