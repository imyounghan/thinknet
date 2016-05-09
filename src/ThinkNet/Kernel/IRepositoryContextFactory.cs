using ThinkLib.Common;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 创建仓储上下文的工厂
    /// </summary>
    //[UnderlyingComponent(typeof(RepositoryContextFactory))]
    public interface IRepositoryContextFactory
    {
        /// <summary>
        /// 创建一个仓储上下文
        /// </summary>
        IRepositoryContext CreateRepositoryContext();
    }
}
