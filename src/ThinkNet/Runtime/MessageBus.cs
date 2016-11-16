﻿using System.Collections.Generic;
using System.Linq;
using ThinkNet.Contracts;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    public class MessageBus : IMessageBus
    {
        private readonly IEnvelopeSender _sender;
        public MessageBus(IEnvelopeSender sender)
        {
            this._sender = sender;
        }


        private Envelope Transform(IMessage message)
        {
            var envelope = new Envelope(message);
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.MessageKind;
            //envelope.Metadata[StandardMetadata.IdentifierId] = message.Id;

            var eventStream = message as EventStream;
            if(eventStream != null) {
                envelope.Metadata[StandardMetadata.SourceId] = eventStream.SourceId;
            }

            var commandResult = message as CommandResultReplied;
            if(commandResult != null) {
                envelope.Metadata[StandardMetadata.SourceId] = commandResult.CommandId;
            }

            var command = message as Messaging.ICommand;
            if(command != null) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = command.UniqueId;
            }

            var @event = message as IEvent;
            if(@event != null) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                envelope.Metadata[StandardMetadata.SourceId] = @event.UniqueId;
            }

            return envelope;
        }
        
        #region IMessageBus 成员

        public void Publish(IMessage message)
        {
            _sender.SendAsync(Transform(message));
        }

        public void Publish(IEnumerable<IMessage> messages)
        {
            _sender.SendAsync(messages.Select(Transform));
        }

        #endregion
    }
}
