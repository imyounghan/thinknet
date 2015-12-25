using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个命令。
    /// </summary>
    public interface ICommand : IMessage
    {
        ///// <summary>
        ///// 获取命令标识id
        ///// </summary>
        //string Id { get; }
        ///// <summary>
        ///// 重试次数。
        ///// </summary>
        //int RetryCount { get; }
        ///// <summary>
        ///// 获取命令的key值。
        ///// </summary>
        //object GetKey();
        /// <summary>
        /// 获取聚合根标识
        /// </summary>
        string AggregateRootId { get; }
    }
}
