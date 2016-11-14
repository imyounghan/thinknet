

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个命令。
    /// </summary>
    public interface ICommand : IMessage
    {
        /// <summary>
        /// 命令的唯一标识
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 生成命令的时间戳
        /// </summary>
        System.DateTime Timestamp { get; }
        ///// <summary>
        ///// 获取聚合根ID
        ///// </summary>
        //string AggregateRootId { get; }
    }
}
