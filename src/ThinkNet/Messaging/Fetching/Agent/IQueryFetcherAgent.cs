using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    /// <summary>
    /// 获取查询结果的代理接口
    /// </summary>
    public interface IQueryFetcherAgent
    {
        /// <summary>
        /// 获取查询结果
        /// </summary>
        IQueryResult Fetch(IQuery parameter);
    }
}
