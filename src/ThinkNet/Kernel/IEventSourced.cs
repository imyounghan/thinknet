using System.Collections.Generic;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示继承该接口的类型是一个由事件溯源的对象
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
        IEnumerable<IVersionedEvent> GetEvents();

        /// <summary>
        /// 通过事件还原对象状态。
        /// </summary>
        void LoadFrom(IEnumerable<IVersionedEvent> events);
    }
}
