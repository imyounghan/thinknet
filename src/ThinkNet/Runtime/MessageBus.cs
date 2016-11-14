using System.Collections.Generic;
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
            if (message is EventStream) {
                envelope.Metadata[StandardMetadata.SourceId] = ((EventStream)message).SourceId;
            }
            else if (message is CommandResultReplied) {
                envelope.Metadata[StandardMetadata.SourceId] = ((CommandResultReplied)message).CommandId;
            }
            else if (message is Messaging.ICommand) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((Messaging.ICommand)message).Id;
            }
            else if (message is IEvent) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                envelope.Metadata[StandardMetadata.SourceId] = ((IEvent)message).Id;
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
