using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkLib.Caching;
using ThinkLib.Common;
using ThinkNet.Configurations;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 设置或获取聚合的缓存接口
    /// </summary>
    [UnderlyingComponent(typeof(MemoryCache))]
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

        private readonly BinaryFormatter _serializer;
        private readonly bool _enabled;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MemoryCache()
        {
            this._serializer = new BinaryFormatter();
            this._enabled = ConfigurationManager.AppSettings["thinkcfg.caching_enabled"].Change(false);
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

            object data = CacheManager.GetCache(cacheRegion).Get(cacheKey);
            if (data == null)
                return null;

            return this.Deserialize((byte[])data);
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

            var data = this.Serialize(entity);
            CacheManager.GetCache(cacheRegion).Put(cacheKey, data);
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

            CacheManager.GetCache(cacheRegion).Remove(cacheKey);
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
