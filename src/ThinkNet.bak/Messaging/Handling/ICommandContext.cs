using ThinkNet.Domain;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 处理命令的上下文接口
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        /// 添加该聚合根到当前上下文中。
        /// </summary>
        void Add(IAggregateRoot aggregateRoot);
        /// <summary>
        /// 从当前上下文获取聚合根，如果不存在，则可能从缓存中缓存，缓存中没有的话则从数据库中获取。
        /// 如果还不存在的话则抛出异常。
        /// </summary>
        /// <exception cref="EntityNotFoundException" />
        T Get<T>(object id) where T : class, IAggregateRoot;

        /// <summary>
        /// 从当前上下文获取聚合根，如果不存在，则可能从缓存中缓存，缓存中没有的话则从数据库中获取。
        /// 如果还不存在的话则返回一个空的引用。
        /// </summary>
        T Find<T>(object id) where T : class, IAggregateRoot;

        /// <summary>
        /// 添加待处理的事件。
        /// </summary>
        void AppendEvent(Event @event);
    }
}
