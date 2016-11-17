using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkNet.Common;
using Caching = ThinkNet.Common.Caching;

namespace ThinkNet.Domain
{
    /// <summary>
    /// <see cref="ICache"/> 的本机缓存
    /// </summary>
    public class LocalCache : ICache
    {
        private readonly Caching.ICacheProvider _cacheProivder;
        private readonly ConcurrentDictionary<string, Caching.ICache> _caches;
        private readonly BinaryFormatter _serializer;
        private readonly bool _enabled;
        private readonly HashSet<string> _keys;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public LocalCache()
        {
            this._serializer = new BinaryFormatter();
            this._enabled = ConfigurationManager.AppSettings["thinkcfg.caching_enabled"].ChangeIfError(false);

            this._cacheProivder = new Caching.MemoryCacheProvider();
            this._caches = new ConcurrentDictionary<string, Caching.ICache>();
            this._keys = new HashSet<string>();
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
        public bool TryGet<T>(Type modelType, object modelId, out T model)
        {
            model = default(T);
            if (!_enabled)
                return false;

            if (modelType.IsAbstract || !modelType.IsClass)
                return false;

            modelType.NotNull("type");
            modelId.NotNull("id");

            string cacheRegion = GetCacheRegion(modelType);
            string cacheKey = BuildCacheKey(modelType, modelId);

            if (_keys.Contains(cacheKey))
                return true;

            object data = GetCache(cacheRegion).Get(cacheKey);
            if (data == null)
                return false;

            var de = (DictionaryEntry)data;
            if (modelId.ToString() == de.Key.ToString()) {
                model = (T)this.Deserialize((byte[])de.Value);
                return true;
            }
            else {
                return false;
            }            
        }
        /// <summary>
        /// 设置实例到缓存
        /// </summary>
        public void Set(object model, object modelId)
        {
            if (!_enabled)
                return;

            //aggregateRoot.NotNull("aggregateRoot");
            modelId.NotNull("modelId");

            var type = model.GetType();

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, modelId);

            if (model == null) {
                _keys.Add(cacheKey);
                return;
            }

            var data = new DictionaryEntry(modelId, this.Serialize(model));
            GetCache(cacheRegion).Put(cacheKey, data);
            //if (cache.Contains(cacheKey, cacheRegion)) {
            //    cache.Set(cacheKey, data, policy, cacheRegion);
            //}
            //else {
            //    cache.Add(cacheKey, data, policy, cacheRegion);
            //}
        }
        /// <summary>
        /// 从缓存中移除
        /// </summary>
        public void Remove(Type modelType, object modelId)
        {
            if (!_enabled)
                return;

            modelType.NotNull("modelType");
            modelId.NotNull("modelId");

            string cacheRegion = GetCacheRegion(modelType);
            string cacheKey = BuildCacheKey(modelType, modelId);

            _keys.Remove(cacheKey);
            GetCache(cacheRegion).Remove(cacheKey);
            //if (cache.Contains(cacheKey, cacheRegion)) {
            //    cache.Remove(cacheKey, cacheRegion);
            //}
        }

        private Caching.ICache GetCache(string region)
        {
            return _caches.GetOrAdd(region, _cacheProivder.BuildCache);
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
            return string.Format("Model:{0}:{1}", type.FullName, key);
        }
    }
}
