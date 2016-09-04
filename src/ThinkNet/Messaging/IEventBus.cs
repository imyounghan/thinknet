using System.Collections.Generic;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是事件总线
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布领域事件
        /// </summary>
        void Publish(IEvent @event);

        /// <summary>
        /// 发布一组事件
        /// </summary>
        void Publish(IEnumerable<IEvent> events);
    }   
}
