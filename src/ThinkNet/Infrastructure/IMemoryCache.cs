using System;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 设置或获取聚合的缓存接口
    /// </summary>
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
}
