using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Messaging;

namespace ThinkNet.Domain
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
        [DataMember(Name = "version")]
        public int Version { get; private set; }

        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        new protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event<TIdentify>
        {
            var eventType = @event.GetType();
            var eventSourcedType = this.GetType();
            if (!AggregateRootInnerHandler.HasHandler(eventSourcedType, eventType)) {
                var errorMessage = string.Format("Event handler not found on {0} for {1}.",
                    eventSourcedType.FullName, eventType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            base.RaiseEvent(@event);
        }


        //private void ApplyEvent(IEvent @event)
        //{
        //    var eventType = @event.GetType();
        //    var eventSourcedType = this.GetType();
        //    var handler = AggregateRootInnerHandler.GetHandler(eventSourcedType, eventType);

        //    if (handler == null) {
        //        var errorMessage = string.Format("Event handler not found on {0} for {1}.",
        //            eventSourcedType.FullName, eventType.FullName);
        //        throw new ThinkNetException(errorMessage);
        //    }

        //    handler(this, @event);
        //}

        #region IEventSourced 成员
        //[IgnoreDataMember]
        //EventStream IEventSourced.EventStream
        //{
        //    get
        //    {
        //        return new EventStream() {
        //            Events = this.GetEvents(),
        //            SourceId = this.Id.ToString(),
        //            SourceType = this.GetType(),
        //            Version = this.Version + 1
        //        };
        //    }
        //}

        //void IEventSourced.LoadFrom(IEnumerable<EventStream> eventStreams)
        //{
        //    var currentType = this.GetType();
        //    foreach (var eventStream in eventStreams) {
        //        if (eventStream.SourceType != null && eventStream.SourceType != currentType) {
        //            var errorMessage = string.Format("Cannot load because the VersionedEvent sourcetype '{0}' is not equal to the AggregateRoot type '{1}'.",
        //                eventStream.SourceType.FullName, currentType.FullName);
        //            throw new ThinkNetException(errorMessage);
        //        }

        //        if (this.Version == 0 && this.Id.Equals(default(TIdentify))) {
        //            this.Id = (TIdentify)eventStream.SourceId.Change(typeof(TIdentify));
        //        }
        //        else {
        //            var aggregateRootStringId = this.Id.ToString();
        //            if (eventStream.SourceId != aggregateRootStringId) {
        //                var errorMessage = string.Format("Cannot load because the VersionedEvent sourceid '{0}' is not equal to the AggregateRoot id '{1}'.",
        //                    eventStream.SourceId, aggregateRootStringId);
        //                throw new ThinkNetException(errorMessage);
        //            }
        //        }

        //        if (eventStream.Version != this.Version + 1) {
        //            var errorMessage = string.Format("Cannot load because the VersionedEvent version '{0}' is not equal to the AggregateRoot version '{1}'.",
        //                eventStream.Version, this.Version);
        //            throw new ThinkNetException(errorMessage);
        //        }

        //        this.Version = eventStream.Version;
        //        eventStream.Events.ForEach(this.ApplyEvent);
        //    }
        //}

        void IEventSourced.LoadFrom(IEnumerable<IEvent> events)
        {
            this.Version++;
            events.Cast<Event<TIdentify>>().ForEach(this.RaiseEvent);
            base.ClearEvents();
        }
        #endregion
    }
}
