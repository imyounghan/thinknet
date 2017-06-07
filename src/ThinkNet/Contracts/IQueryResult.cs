

namespace ThinkNet.Contracts
{
    public interface IQueryResult
    {
        /// <summary>
        /// 失败的消息
        /// </summary>
        string ErrorMessage { get; }
        /// <summary>
        /// 查询返回状态。
        /// </summary>
        ExecutionStatus Status { get; }
    }
}
