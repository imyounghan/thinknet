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
            if (!AggregateRootInnerHandlerProvider.Instance.HasHandler(eventSourcedType, eventType)) {
                var errorMessage = string.Format("Event handler not found on {0} for {1}.",
                    eventSourcedType.FullName, eventType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            base.RaiseEvent(@event);
        }

        #region IEventSourced 成员        

        void IEventSourced.LoadFrom(IEnumerable<Event> events)
        {
            this.Version++;
            events.Cast<Event<TIdentify>>().ForEach(this.RaiseEvent);
            base.ClearEvents();
        }
        #endregion
    }
}
