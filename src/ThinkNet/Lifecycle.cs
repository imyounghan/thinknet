
namespace ThinkNet
{
    /// <summary>
    /// 生命周期类型
    /// </summary>
    public enum Lifecycle
    {
        /// <summary>
        /// 每次都构造一个新实例
        /// </summary>
        Transient,
        /// <summary>
        /// 单例
        /// </summary>
        Singleton,
        /// <summary>
        /// 同一个线程
        /// </summary>
        PerThread,
        /// <summary>
        /// 同一个会话
        /// </summary>
        PerSession,
    }
}
