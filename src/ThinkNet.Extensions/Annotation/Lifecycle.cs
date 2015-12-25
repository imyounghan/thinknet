
namespace ThinkNet.Annotation
{
    /// <summary>
    /// 生命周期
    /// </summary>
    public enum Lifecycle : byte
    {
        /// <summary>
        /// 每次都构造一个新实例
        /// </summary>
        Transient,
        /// <summary>
        /// 单例
        /// </summary>
        Singleton,
    }
}
