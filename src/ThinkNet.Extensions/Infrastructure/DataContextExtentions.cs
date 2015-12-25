
namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="IDataContext"/> 的扩展类
    /// </summary>
    public static class DataContextExtentions
    {
        /// <summary>
        /// 获取该类型的实例
        /// </summary>
        public static T Get<T>(this IDataContext dataContext, object id)
            where T : class
        {
            return dataContext.Get(typeof(T), id) as T;
        }
    }
}
