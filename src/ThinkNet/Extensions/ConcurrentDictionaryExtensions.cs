using System.Collections.Concurrent;

namespace ThinkNet
{
    /// <summary>
    /// ConcurrentDictionary的扩展
    /// </summary>
    public static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// 删除存在key的元素
        /// </summary>
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryRemove(key);
        }

        /// <summary>
        /// 删除存在key的元素
        /// </summary>
        public static TValue RemoveAndGet<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null || dict.Count == 0)
                return default(TValue);

            TValue value;
            if (dict.TryRemove(key, out value)) {
                return value;
            }

            return default(TValue);
        }

        /// <summary>
        /// 删除存在key的元素
        /// </summary>
        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict == null || dict.Count == 0)
                return false;

            TValue value;
            return dict.TryRemove(key, out value);
        }

        /// <summary>
        /// 获取key的元素
        /// </summary>
        public static TValue GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            TValue value;
            if (dict.TryGetValue(key, out value)) {
                return value;
            }

            return default(TValue);
        }
    }
}
