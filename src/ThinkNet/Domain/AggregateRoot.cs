using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using ThinkLib;
using ThinkNet.Messaging;


namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示一个通过事件溯源的聚合根的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : Entity<TIdentify>, IEventSourced
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
        [DataMember(Name = "version")]
        public int Version { get; private set; }


        [NonSerialized]
        [IgnoreDataMember]
        private IList<Event> _pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event<TIdentify>
        {
            @event.SourceId = this.Id;

            if (_pendingEvents == null) {
                _pendingEvents = new List<Event>();
            }
            _pendingEvents.Add(@event);

            this.ApplyEvent(@event);
        }

        private void ApplyEvent(Event @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = this.GetType();
            Action<IAggregateRoot, Event> innerHandler;
            if(AggregateRootInnerHandlerProvider.Instance.TryGetHandler(aggregateRootType, eventType, out innerHandler)) {
                innerHandler.Invoke(this, @event);
            }
            else {
                var errorMessage = string.Format("Event handler not found on {0} for {1}.",
                    aggregateRootType.FullName, eventType.FullName);
                throw new ThinkNetException(errorMessage);
            }
        }

        /// <summary>
        /// 获取待发布的事件列表。
        /// </summary>
        protected IEnumerable<Event> GetEvents()
        {
            if (_pendingEvents == null || _pendingEvents.Count == 0) {
                return Enumerable.Empty<Event>();
            }
            return new ReadOnlyCollection<Event>(_pendingEvents);
        }

        /// <summary>
        /// 清除事件。
        /// </summary>
        protected void ClearEvents()
        {
            if (_pendingEvents != null) {
                _pendingEvents.Clear();
                _pendingEvents = null;
            }
        }       
        

       
        #region IEventPublisher 成员
        [IgnoreDataMember]
        IEnumerable<Event> IEventPublisher.Events
        {
            get
            {
                if(_pendingEvents == null || _pendingEvents.Count == 0) {
                    return Enumerable.Empty<Event>();
                }
                var array = new Event[_pendingEvents.Count];
                _pendingEvents.CopyTo(array, 0);
                this.Version++;
                this.ClearEvents();
                return array;
            }
        }

        #endregion

        #region IAggregateRoot 成员
        [IgnoreDataMember]
        object IAggregateRoot.Id
        {
            get { return this.Id; }
        }

        [IgnoreDataMember]
        bool IEventSourced.IsChanged
        {
            get { return _pendingEvents != null && _pendingEvents.Count > 0; }
        }
        #endregion

        #region IEventSourced 成员
        void IEventSourced.LoadFrom(IEnumerable<Event> events)
        {
            this.Version++;
            events.ForEach(this.ApplyEvent);
        }
        #endregion
    }
}
