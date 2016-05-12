using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using ThinkLib.Logging;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// 实现 <see cref="IAggregateRoot"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : Entity<TIdentify>, IEventSourced, IEventPublisher, ICloneable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected AggregateRoot()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected AggregateRoot(TIdentify id)
            : base(id)
        { }

        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember]
        public int Version { get; private set; }


        [NonSerialized]
        private ICollection<IEvent> pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : IEvent
        {
            if (@event is VersionedEvent<TIdentify>)
                HandleVersionedEvent(@event as VersionedEvent<TIdentify>);
            else if (@event is Event<TIdentify>)
                HandleDomainEvent(@event as Event<TIdentify>);
            else
                HandleEvent(@event);


            if (pendingEvents == null) {
                pendingEvents = new List<IEvent>();
            }
            pendingEvents.Add(@event);
        }

        private void HandleDomainEvent(Event<TIdentify> @event)
        {
            @event.SourceId = this.Id;
            this.HandleEvent(@event);
        }

        private void HandleVersionedEvent(VersionedEvent<TIdentify> @event)
        {
            @event.Version = this.Version + 1;
            this.HandleDomainEvent(@event);
            this.Version = @event.Version;
        }

        private void HandleEvent(IEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = this.GetType();
            var handler = AggregateRootInnerHandlerProvider.GetEventHandler(aggregateRootType, eventType);
            if (handler == null) {
                string errorMessage = string.Format("Event handler not found on {0} for {1}.",
                    aggregateRootType.FullName, eventType.FullName);
                if (@event is VersionedEvent<TIdentify>) {
                    throw new EventSourcedException(errorMessage);
                }

                var log = LogManager.GetLogger("ThinkZoo");
                if (log.IsWarnEnabled)
                    log.Warn(errorMessage);
                return;
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

        /// <summary>
        /// 获取待发布的事件列表。
        /// </summary>
        protected IEnumerable<IEvent> GetPendingEvents()
        {
            if (pendingEvents == null) {
                pendingEvents = new List<IEvent>();
            }
            return this.pendingEvents;
        }

        /// <summary>
        /// 清除事件。
        /// </summary>
        protected void ClearEvents()
        {
            if (pendingEvents != null) {
                pendingEvents.Clear();
                pendingEvents = null;
            }
        }

        //public TRole ActAs<TRole>() where TRole : class
        //{
        //    if (!typeof(TRole).IsInterface) {
        //        throw new AggregateRootException(string.Format("'{0}' is not an interface type.", typeof(TRole).FullName));
        //    }

        //    var actor = this as TRole;

        //    if (actor == null) {
        //        throw new AggregateRootException(string.Format("'{0}' cannot act as role '{1}'.", GetType().FullName, typeof(TRole).FullName));
        //    }

        //    return actor;
        //}

        /// <summary>
        /// 克隆对象
        /// </summary>
        protected virtual object Clone()
        {
            return this.Change(this.GetType());
        }



        #region IEventSourced 成员

        IEnumerable<IVersionedEvent> IEventSourced.GetEvents()
        {
            return this.GetPendingEvents().OfType<IVersionedEvent>();
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

        #region IAggregateRoot 成员

        [IgnoreDataMember]
        object IAggregateRoot.Id
        {
            get
            {
                return this.Id;
            }
        }

        #endregion

        #region IEventPublisher 成员
        [IgnoreDataMember]
        IEnumerable<IEvent> IEventPublisher.Events
        {
            get
            {
                return this.GetPendingEvents();
            }
        }

        #endregion

        #region ICloneable 成员

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #endregion
    }
}
