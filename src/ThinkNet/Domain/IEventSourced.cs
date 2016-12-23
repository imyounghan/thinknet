using System.Collections.Generic;
using ThinkNet.Messaging;

namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示继承该接口的是一个通过事件溯源的聚合根。
    /// </summary>
    public interface IEventSourced : IAggregateRoot, IEventPublisher
    {
        /// <summary>
        /// 表示当前的版本号
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 表示当前状态是否有变化
        /// </summary>
        bool IsChanged { get; }

        /// <summary>
        /// 变更版本号
        /// </summary>
        void AcceptChanges(int newVersion);

        /// <summary>
        /// 通过事件还原对象状态。
        /// </summary>
        void LoadFrom(IEnumerable<Event> events);
    }
}
