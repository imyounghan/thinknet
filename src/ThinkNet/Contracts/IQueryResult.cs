namespace ThinkNet.Contracts
{
    /// <summary>
    /// 继承该接口的是一个查询结果
    /// </summary>
    public interface IQueryResult
    {
        /// <summary>
        /// 失败的消息编码
        /// </summary>
        string ErrorCode { get; }
        /// <summary>
        /// 失败的消息
        /// </summary>
        string ErrorMessage { get; }
        /// <summary>
        /// 查询返回状态。
        /// </summary>
        QueryStatus Status { get; }
    }
}
