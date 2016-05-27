using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 实现 <see cref="IEventSourced"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class EventSourced<TIdentify> : AggregateRoot<TIdentify>, IEventSourced
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected EventSourced()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected EventSourced(TIdentify id)
            : base(id)
        { }

        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember]
        public int Version { get; private set; }


        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        new protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : VersionedEvent<TIdentify>
        {
            @event.NotNull("@event");

            @event.Version = this.Version + 1;
            base.RaiseEvent(@event);
            this.HandleEvent(@event);
            this.Version = @event.Version;
        }

        private void HandleEvent(IVersionedEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = this.GetType();
            var handler = AggregateRootInnerHandlerProvider.GetEventHandler(aggregateRootType, eventType);
            if (handler == null) {
                string errorMessage = string.Format("Event handler not found on {0} for {1}.",
                    aggregateRootType.FullName, eventType.FullName);
                throw new EventSourcedException(errorMessage);
            }
            handler(this, @event);
        }

        private void CheckEvent(IVersionedEvent @event)
        {
            if (@event.Version == 1 && this.Id.Equals(default(TIdentify)))
                this.Id = @event.SourceId.Change<TIdentify>();

            if (@event.Version > 1 && this.Id.ToString() != @event.SourceId)
                throw new EventSourcedException(@event.SourceId, this.Id.ToString());

            if (@event.Version != this.Version + 1)
                throw new EventSourcedException(@event.Version, this.Version);
        }

        #region IEventSourced 成员

        IEnumerable<IVersionedEvent> IEventSourced.GetEvents()
        {
            return base.GetPendingEvents().OfType<IVersionedEvent>();
        }


        void IEventSourced.LoadFrom(IEnumerable<IVersionedEvent> events)
        {
            foreach (var @event in events) {
                this.CheckEvent(@event);
                this.HandleEvent(@event);
                this.Version = @event.Version;
            }
        }

        #endregion
    }
}
