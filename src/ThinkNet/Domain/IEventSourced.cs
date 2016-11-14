using System.Collections.Generic;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;

namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示继承该接口的是一个通过事件溯源的聚合根。
    /// </summary>
    public interface IEventSourced : IAggregateRoot, IEventPublisher
    {
        /// <summary>
        /// 版本号
        /// </summary>
        int Version { get; }

        ///// <summary>
        ///// 获取命令处理聚合的事件
        ///// </summary>
        //EventStream EventStream { get; }

        /// <summary>
        /// 通过事件还原对象状态。
        /// </summary>
        void LoadFrom(IEnumerable<IEvent> events);
    }
}
