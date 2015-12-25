using System.Collections.Generic;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继续该接口的是一个事件发布程序。
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 获取未发布的事件。
        /// </summary>
        IEnumerable<IEvent> Events { get; }
    }
}
