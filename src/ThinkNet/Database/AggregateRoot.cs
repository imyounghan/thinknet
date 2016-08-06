using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Messaging;


namespace ThinkNet.Database
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
        [DataMember]
        public virtual TIdentify Id { get; protected set; }



        [NonSerialized]
        private ICollection<Event<TIdentify>> pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : Event<TIdentify>
        {
            @event.SourceId = this.Id;

            if (pendingEvents == null) {
                pendingEvents = new List<Event<TIdentify>>();
            }
            pendingEvents.Add(@event);
        }

        /// <summary>
        /// 获取待发布的事件列表。
        /// </summary>
        protected IEnumerable<Event<TIdentify>> GetPendingEvents()
        {
            if (pendingEvents == null) {
                pendingEvents = new List<Event<TIdentify>>();
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
            get { return this.GetPendingEvents().Cast<IEvent>(); }
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
