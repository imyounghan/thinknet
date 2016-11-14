
namespace ThinkNet.Common.Context
{
    /// <summary>
    /// 表示继承该接口的是一个上下文
    /// </summary>
    public interface IContext : System.IDisposable
    {
        /// <summary>
        /// 获取当前的Manager
        /// </summary>
        IContextManager ContextManager { get; }
    }
}
