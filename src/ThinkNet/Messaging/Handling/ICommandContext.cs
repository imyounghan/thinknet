using ThinkNet.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示这是一个处理命令的上下文接口
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        /// 添加该聚合根到当前上下文中。
        /// </summary>
        void Add(IEventSourced aggregateRoot);
        /// <summary>
        /// 从当前上下文获取聚合根，如果不存在，则可能从缓存中缓存，缓存中没有的话则从 <see cref="ThinkNet.Infrastructure.IEventStore"/> 中获取。
        /// 如果还不存在的话则抛出异常。
        /// </summary>
        T Get<T>(object id) where T : class, IEventSourced;

        /// <summary>
        /// 从当前上下文获取聚合根，如果不存在，则可能从缓存中缓存，缓存中没有的话则从 <see cref="ThinkNet.Infrastructure.IEventStore"/> 中获取。
        /// 如果还不存在的话则返回一个空的引用。
        /// </summary>
        T Find<T>(object id) where T : class, IEventSourced;

        /// <summary>
        /// 待处理的事件。
        /// </summary>
        void PendingEvent(IEvent @event);
        /// <summary>
        /// 提交当前聚合根的修改
        /// </summary>
        void Commit(string commandId);
    }
}
