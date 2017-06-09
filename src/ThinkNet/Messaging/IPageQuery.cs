

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是一个分页查询命令
    /// </summary>
    public interface IPageQuery : IQuery
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// 当前页显示数量大小
        /// </summary>
        int PageSize { get; }
    }
}
