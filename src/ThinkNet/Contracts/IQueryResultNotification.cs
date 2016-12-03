
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示查询结果的通知接口
    /// </summary>
    public interface IQueryResultNotification
    {
        /// <summary>
        /// 通知查询结果
        /// </summary>
        void Notify(string queryId, IQueryResult queryResult);
    }
}
