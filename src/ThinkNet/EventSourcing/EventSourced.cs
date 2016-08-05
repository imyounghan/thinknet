using System;
using System.Collections.Generic;
using System.Linq;
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
            HandleEvent(@event);
            base.RaiseEvent(@event);
        }

        private void HandleEvent(IEvent @event)
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

        void IEventSourced.LoadFrom(int version, IEnumerable<IEvent> events)
        {
            if (version != this.Version + 1)
                throw new EventSourcedException(version, this.Version);

            this.Version = version;

            var aggregateRootStringId = this.Id.ToString();
            foreach (var @event in events.Cast<Event<TIdentify>>()) {
                var eventSourceStringId = @event.SourceId.ToString();
                if (eventSourceStringId != aggregateRootStringId)
                    throw new EventSourcedException(eventSourceStringId, aggregateRootStringId);

                this.HandleEvent(@event);
            }
        }

        #endregion
    }
}
