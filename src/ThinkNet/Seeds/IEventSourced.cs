
namespace ThinkNet.Seeds
{
    using System.Collections.Generic;

    using ThinkNet.Messaging;

    /// <summary>
    /// 表示继承该接口的是一个通过事件溯源当前状态的聚合根
    /// </summary>
    public interface IEventSourced : IAggregateRoot, IEventPublisher
    {
        /// <summary>
        /// 表示当前的版本号
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 接受变更版本号
        /// </summary>
        void AcceptChanges(int newVersion);

        /// <summary>
        /// 加载事件。
        /// </summary>
        void LoadFrom(IEnumerable<Event> events);
    }
}
