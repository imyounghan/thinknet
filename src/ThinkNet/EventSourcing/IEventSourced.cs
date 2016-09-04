using System.Collections.Generic;
using ThinkNet.Database;
using ThinkNet.Messaging;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示继承该接口的是一个通过事件溯源的聚合根。
    /// </summary>
    public interface IEventSourced : IAggregateRoot
    {
        /// <summary>
        /// 版本号
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 获取溯源事件
        /// </summary>
        IEnumerable<IEvent> GetEvents();
        /// <summary>
        /// 清除事件
        /// </summary>
        void ClearEvents();

        /// <summary>
        /// 通过事件还原对象状态。
        /// </summary>
        void LoadFrom(IEnumerable<VersionedEvent> events);
    }
}
