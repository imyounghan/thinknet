using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个事件。
    /// </summary>
    public interface IEvent : IMessage
    {
        ///// <summary>
        ///// 获得当前事件的唯一标识符
        ///// </summary>
        //string Id { get; }
        ///// <summary>
        ///// 重试次数。
        ///// </summary>
        //int RetryCount { get; }
        ///// <summary>
        ///// 获取事件的key值。
        ///// </summary>
        //object GetKey();
        /// <summary>
        /// 领域事件的来源id
        /// </summary>
        string SourceId { get; }
    }
}
