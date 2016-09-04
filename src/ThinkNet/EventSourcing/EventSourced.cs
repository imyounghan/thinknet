using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ThinkNet.Database;
using ThinkNet.Messaging;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 实现 <see cref="IEventSourced"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class EventSourced<TIdentify> : AggregateRoot<TIdentify>, IEventSourced
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected EventSourced(TIdentify id)
            : base(id)
        {
            this.Id = id;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember]
        public int Version { get; private set; }

        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        new protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event<TIdentify>
        {
            this.ApplyEvent(@event);
            base.RaiseEvent(@event);
        }

        private void ApplyEvent(IEvent @event)
        {
            var eventType = @event.GetType();
            var eventSourcedType = this.GetType();
            var handler = EventSourcedInnerHandlerProvider.GetEventHandler(eventSourcedType, eventType);

            if (handler == null)
                throw new EventSourcedException(eventType, eventSourcedType);

            handler(this, @event);
        }


        #region IEventSourced 成员

        IEnumerable<IEvent> IEventSourced.GetEvents()
        {
            return this.GetPendingEvents();
        }

        [NonSerialized]
        private bool changed;
        void IEventSourced.ClearEvents()
        {
            if (!this.changed) {
                this.Version++;
                this.changed = true;
            }
            base.ClearEvents();
        }

        void IEventSourced.LoadFrom(IEnumerable<VersionedEvent> events)
        {
            foreach(var @event in events) {
                if(@event.Version != this.Version + 1)
                    throw new EventSourcedException(@event.Version, this.Version);

                var aggregateRootStringId = this.Id.ToString();
                if(@event.SourceId != aggregateRootStringId) {
                    throw new EventSourcedException(@event.SourceId, aggregateRootStringId);
                }
                this.Version = @event.Version;
                @event.Events.ForEach(this.ApplyEvent);
            }
        }

        #endregion
    }
}
