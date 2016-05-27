using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// 实现 <see cref="IAggregateRoot"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : Entity<TIdentify>, IAggregateRoot, IEventPublisher, ICloneable
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


        [NonSerialized]
        private ICollection<IEvent> pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event<TIdentify>
        {
            @event.SourceId = this.Id;

            if (pendingEvents == null) {
                pendingEvents = new List<IEvent>();
            }
            pendingEvents.Add(@event);
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


        /// <summary>
        /// 克隆对象
        /// </summary>
        protected virtual object Clone()
        {
            return this.Change(this.GetType());
        }


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
