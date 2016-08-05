using System;
using ThinkNet.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>Represents a command context for aggregate command handler handling command.
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>Add a new aggregate into the current command context.
        /// </summary>
        /// <param name="aggregateRoot"></param>
        void Add(IEventSourced aggregateRoot);
        /// <summary>Get the aggregate from memory cache, if not exist, then get it from event store.
        /// </summary>
        T Get<T>(object id) where T : class, IEventSourced;

        T Find<T>(object id) where T : class, IEventSourced;

        void PendingEvent(IEvent @event);

        void Commit(string commandId);
    }
}
