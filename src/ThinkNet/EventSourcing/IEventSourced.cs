using System.Collections.Generic;
using ThinkNet.Database;
using ThinkNet.Messaging;

namespace ThinkNet.EventSourcing
{
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
        void LoadFrom(int version, IEnumerable<IEvent> events);
    }
}
