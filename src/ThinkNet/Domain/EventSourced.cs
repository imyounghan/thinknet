using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ThinkNet.Messaging;

namespace ThinkNet.Domain
{
    public abstract class EventSourced<TIdentify> : AggregateRoot<TIdentify>, IEventSourced
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected EventSourced()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected EventSourced(TIdentify id)
            : base(id)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected EventSourced(TIdentify id, int version)
            : base(id)
        {
            this.Version = version;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember(Name = "version")]
        public int Version { get; private set; }


        #region IEventSourced 成员
        [IgnoreDataMember]
        bool IEventSourced.IsChanged
        {
            get { return !this.GetEvents().IsEmpty(); }
        }
        #endregion

        #region IEventSourced 成员
        void IEventSourced.LoadFrom(IEnumerable<Event> events)
        {
            this.Version++;
            events.ForEach(this.ApplyEvent);
        }

        void IEventSourced.AcceptChanges(int newVersion)
        {
            if(this.Version + 1 != newVersion) {
                throw new InvalidOperationException(string.Format("Cannot accept invalid version: {0}, expect version: {1}, current aggregateRoot type: {2}, id: {3}", newVersion, this.Version + 1, this.GetType().FullName, this.Id));
            }
            this.Version = newVersion;
            this.ClearEvents();
        }

        #endregion
    }
}
