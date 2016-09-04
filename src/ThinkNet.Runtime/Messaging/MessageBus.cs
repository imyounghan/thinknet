using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;
using ThinkNet.EventSourcing;

namespace ThinkNet.Messaging
{
    internal class MessageBus : CommandService, ICommandNotification, ICommandBus, IEventBus
    {
        private readonly IEnvelopeSender _sender;
        public MessageBus(IEnvelopeSender sender)
        {
            this._sender = sender;
        }
        
        private Envelope Transform(IMessage message)
        {
            var envelope = new Envelope(message);
            envelope.Metadata[StandardMetadata.IdentifierId] = message.Id;
            if (message is VersionedEvent) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.VersionedEventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((VersionedEvent)message).SourceId;
            }
            else if(message is RepliedCommand) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.RepliedCommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((RepliedCommand)message).CommandId;
            }
            else if (message is ICommand) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((ICommand)message).AggregateRootId;
            }
            else if (message is IEvent) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((IEvent)message).SourceId;
            }

            return envelope;
        }

        public override Task SendAsync(ICommand command)
        {
            return _sender.SendAsync(Transform(command));
        }

        public virtual void Send(IEnumerable<ICommand> commands)
        {
            //foreach(var command in commands) {
            //    _broker.Add(command);
            //}
            _sender.SendAsync(commands.Select(Transform));
        }

        #region IEventBus 成员
        public virtual void Publish(IEnumerable<IEvent> events)
        {
            //foreach(var @event in events) {
            //    _broker.Add(@event);
            //}
            _sender.SendAsync(events.Select(Transform));
        }

        public void Publish(IEvent @event)
        {
            _sender.SendAsync(Transform(@event));
        }

        #endregion

        #region ICommandNotification 成员

        public void NotifyCompleted(string commandId, Exception exception = null)
        {
            this.NotifyCommandCompleted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyHandled(string commandId, Exception exception = null)
        {
            this.NotifyCommandExecuted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyUnchanged(string commandId)
        {
            this.NotifyCommandCompleted(commandId, CommandStatus.NothingChanged, null);
        }

        #endregion
    }
}
