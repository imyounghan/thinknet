
namespace ThinkNet.Database.Context
{
    /// <summary>
    /// 当前访问的上下文接口
    /// </summary>
    public interface ICurrentContext
    {
        /// <summary>
        /// 获取当前的上下文
        /// </summary>
        IContext GetContext();
    }
}
