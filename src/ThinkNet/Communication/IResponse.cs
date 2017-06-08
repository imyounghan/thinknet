
namespace ThinkNet.Communication
{
    /// <summary>
    /// 表示一个响应
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// 返回状态。
        /// </summary>
        int Status { get; }
        /// <summary>
        /// 错误消息
        /// </summary>
        string ErrorMessage { get; }
        /// <summary>
        /// 错误编码
        /// </summary>
        string ErrorCode { get; }
        /// <summary>
        /// 返回结果的文本
        /// </summary>
        string Result { get; }
        /// <summary>
        /// 返回结果的类型
        /// </summary>
        string ResultType { get; }
    }
}
