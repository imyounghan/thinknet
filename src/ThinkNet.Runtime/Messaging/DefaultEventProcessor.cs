using System;
using System.Collections.Generic;

using ThinkNet.Core;
using ThinkNet.Infrastructure;
using ThinkNet.Runtime.Serialization;


namespace ThinkNet.Messaging.Runtime
{
    /// <summary>
    /// 事件任务处理器
    /// </summary>
    public class DefaultEventProcessor :  MessageProcessor<IEvent>, IEventProcessor
    {
        private readonly ICommandResultManager _commandResultManager;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public DefaultEventProcessor(IEventExecutor eventExecutor,
            IMessageStore messageStore, 
            ITextSerializer serializer, 
            ICommandResultManager commandResultManager,
            IEventPublishedVersionStore eventPublishedVersionStore)
            : base("Event", 4, eventExecutor, messageStore, serializer)
        {
            this._commandResultManager = commandResultManager;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
        }

        protected override bool CheckOrderly(IEvent @event)
        {
            var stream = @event as VersionedEventStream;
            if (stream != null) {
                var version = _eventPublishedVersionStore.GetPublishedVersion(stream.AggregateRootType, stream.AggregateRootId);

                return version + 1 == stream.StartVersion;
            }

            return true;
        }

        protected override void Process(IEvent @event)
        {
            var stream = @event as DomainEventStream;

            try {
                base.Process(@event);


                if (stream != null) {
                    if (stream.Events.IsEmpty()) {
                        _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.NothingChanged, null);
                    }
                    else {
                        _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.Success, null);
                    }
                }
            }
            catch (Exception ex) {
                if (stream != null) {
                    _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.Failed, ex);
                }
                throw ex;
            }
        }
    }
}
