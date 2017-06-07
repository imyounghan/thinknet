

namespace ThinkNet.Seeds
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using ThinkNet.Messaging;
    using ThinkNet.Messaging.Handling;

    /// <summary>
    /// <see cref="IEventSourced"/> 的抽象实现类
    /// </summary>
    [DataContract]
    [Serializable]
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
        /// 版本号
        /// </summary>
        [DataMember(Name = "version")]
        public int Version { get; internal set; }

        internal override bool ApplyEvent(IEvent @event)
        {
            var applied = base.ApplyEvent(@event);

            if (!applied)
            {
                throw new HandlerNotFoundException(this.GetType(), @event.GetType());
            }

            return applied;
        }

        #region IEventSourced 成员


        void IEventSourced.LoadFrom(IEnumerable<IEvent> events)
        {
            //foreach(var @event in events) {
            //    if(@event.Version != this.Version + 1) {
            //        var errorMessage = string.Format("Cannot load because the version '{0}' is not equal to the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
            //            @event.Version, this.Version, this.GetType().FullName, this.Id);
            //        throw new ApplicationException(errorMessage);
            //    }

            //    this.ApplyEvent(@event);
            //}
            events.ForEach(this.RaiseEvent);
            //this.Version++;
            //this.ClearEvents();
        }


        void IEventSourced.AcceptChanges(int newVersion)
        {
            if(this.Version + 1 != newVersion)
            {
                var errorMessage =
                    string.Format(
                        "Cannot accept invalid version: {0}, expect version: {1}, current aggregateRoot type: {2}, id: {3}",
                        newVersion,
                        this.Version + 1,
                        this.GetType().FullName,
                        this.Id);
                throw new ApplicationException(errorMessage);
            }
            this.Version = newVersion;
            this.ClearEvents();
        }
        #endregion

    }
}
