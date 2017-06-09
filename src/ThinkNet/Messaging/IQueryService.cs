
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示这是一个查询服务
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// 读取数据
        /// </summary>
        IQueryResult Execute(IQuery query);

        /// <summary>
        /// 读取数据
        /// </summary>
        IQueryResult<T> Execute<T>(IQuery query);
    }
}
