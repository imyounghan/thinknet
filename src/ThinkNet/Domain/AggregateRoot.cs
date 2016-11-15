using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using ThinkNet.Messaging;


namespace ThinkNet.Domain
{
    /// <summary>
    /// 实现 <see cref="IAggregateRoot"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : IAggregateRoot, IEventPublisher
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
        {
            this.Id = id;
        }

        /// <summary>
        /// 标识ID
        /// </summary>
        [DataMember(Name = "id")]
        public virtual TIdentify Id { get; protected set; }


        [NonSerialized]
        [IgnoreDataMember]
        private IList<IEvent> _pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            var domainEvent = @event as Event<TIdentify>;
            if (domainEvent != null) {
                domainEvent.SourceId = this.Id;
            }
            
            if (_pendingEvents == null) {
                _pendingEvents = new List<IEvent>();
            }
            _pendingEvents.Add(@event);

            //this.ApplyEvent(@event);
            AggregateRootInnerHandler.Handle(this, @event);
        }

        [NonSerialized]
        [IgnoreDataMember]
        private readonly static ReadOnlyCollection<IEvent> emptyEventCollection = new ReadOnlyCollection<IEvent>(new List<IEvent>());
        /// <summary>
        /// 获取待发布的事件列表。
        /// </summary>
        protected IEnumerable<IEvent> GetEvents()
        {
            if (_pendingEvents == null || _pendingEvents.Count == 0) {
                return emptyEventCollection;
            }
            return new ReadOnlyCollection<IEvent>(_pendingEvents);
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

        /// <summary>
        /// 将该对象演译成<typeparam name="TRole" />。
        /// </summary>
        public TRole ActAs<TRole>() where TRole : class
        {
            if (!typeof(TRole).IsInterface) {
                throw new ThinkNetException(string.Format("'{0}' is not an interface type.", typeof(TRole).FullName));
            }

            var actor = this as TRole;

            if (actor == null) {
                throw new ThinkNetException(string.Format("'{0}' cannot act as role '{1}'.",
                    this.GetType().FullName, typeof(TRole).FullName));
            }

            return actor;
        }


        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            return (obj as AggregateRoot<TIdentify>).Id.Equals(this.Id);
        }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <summary>
        /// 输出字符串格式
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}", this.GetType().FullName, this.Id);
        }

        /// <summary>
        /// 判断两个实例是否相同
        /// </summary>
        public static bool operator ==(AggregateRoot<TIdentify> left, AggregateRoot<TIdentify> right)
        {
            return IsEqual(left, right);
        }

        /// <summary>
        /// 判断两个实例是否不相同
        /// </summary>
        public static bool operator !=(AggregateRoot<TIdentify> left, AggregateRoot<TIdentify> right)
        {
            return !(left == right);
        }


        private static bool IsEqual(AggregateRoot<TIdentify> left, AggregateRoot<TIdentify> right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null)) {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        

       
        #region IEventPublisher 成员
        [IgnoreDataMember]
        IEnumerable<IEvent> IEventPublisher.Events
        {
            get { return this.GetEvents(); }
        }

        #endregion

        #region IAggregateRoot 成员
        [IgnoreDataMember]
        object IAggregateRoot.Id
        {
            get { return this.Id; }
        }

        #endregion
    }
}
