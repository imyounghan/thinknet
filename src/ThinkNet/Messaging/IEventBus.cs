using System.Collections.Generic;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是事件总线
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish(IEvent @event);
        /// <summary>
        /// 发布一组事件(分布式情况下可能会延迟发送)
        /// </summary>
        void Publish(IEnumerable<IEvent> events);

        ///// <summary>
        ///// 异步发布一个事件
        ///// </summary>
        //Task PublishAsync(IEvent @event);
        ///// <summary>
        ///// 异步发布一组事件
        ///// </summary>
        //Task PublishAsync(IEnumerable<IEvent> events);
    }   
}
