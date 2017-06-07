

namespace ThinkNet.Seeds
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Runtime.Serialization;

    using ThinkNet.Messaging;

    /// <summary>
    /// <see cref="IAggregateRoot"/> 的抽象实现类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : Entity<TIdentify>, IAggregateRoot, IEventPublisher
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


        [IgnoreDataMember]
        [NonSerialized]
        private List<IEvent> _pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent(IEvent @event)
        {
            this.ApplyEvent(@event);

            if(_pendingEvents == null) {
                _pendingEvents = new List<IEvent>();
            }
            _pendingEvents.Add(@event);
        }

        internal virtual bool ApplyEvent(IEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = this.GetType();

            Action<IAggregateRoot, IEvent> innerHandler;
            if(InnerHandlerProvider.Instance.TryGetHandler(aggregateRootType, eventType, out innerHandler)) {
                innerHandler.Invoke(this, @event);
                return true;
            }

            return false;
        }


        protected void ClearEvents()
        {
            if(_pendingEvents == null || _pendingEvents.Count == 0) {
                return;
            }

            _pendingEvents.Clear();
        }

        #region IEventPublisher 成员
        IEnumerable<IEvent> IEventPublisher.GetEvents()
        {
            if(_pendingEvents == null || _pendingEvents.Count == 0) {
                return Enumerable.Empty<IEvent>();
            }
            return new ReadOnlyCollection<IEvent>(_pendingEvents);
        }
        #endregion
    }
}
