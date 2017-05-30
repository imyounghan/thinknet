
namespace ThinkNet.Infrastructure.Caching
{
    /// <summary>
    /// 缓存接口
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// 从缓存获取对象实例。
        /// </summary>
        object Get(string key);

        /// <summary>
        /// 放此对象实例放入缓存。
        /// </summary>
        void Put(string key, object value);

        /// <summary>
        /// 从缓存移除该键值对应的对象实例。
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// 清空缓存。
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取此缓存的区域名称。
        /// </summary>
        string RegionName { get; }
    }
}
