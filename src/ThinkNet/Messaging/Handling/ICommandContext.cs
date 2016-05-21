using ThinkLib.Common;
using ThinkNet.Kernel;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>Represents a command context for aggregate command handler handling command.
    /// </summary>
    public interface ICommandContext// : IUnitOfWork
    {
        /// <summary>Add a new aggregate into the current command context.
        /// </summary>
        /// <param name="aggregateRoot"></param>
        void Add(IAggregateRoot aggregateRoot);
        /// <summary>Get the aggregate from memory cache, if not exist, then get it from event store.
        /// </summary>
        T Get<T>(object id) where T : class, IAggregateRoot;

        T Find<T>(object id) where T : class, IAggregateRoot;

        void PendingEvent(IEvent @event);

        void Commit(string commandId);
    }
}
