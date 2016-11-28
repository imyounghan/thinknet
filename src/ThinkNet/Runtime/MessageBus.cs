using System.Collections.Generic;
using System.Linq;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// <see cref="IMessageBus"/> 的实现类
    /// </summary>
    public class MessageBus : IMessageBus
    {
        private readonly IEnvelopeSender _sender;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
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
                envelope.Metadata[StandardMetadata.SourceId] = eventStream.SourceId.Id;
            }

            var commandResult = message as CommandResult;
            if(commandResult != null) {
                envelope.Metadata[StandardMetadata.SourceId] = commandResult.CommandId;
            }

            var command = message as Command;
            if(command != null) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
                envelope.Metadata[StandardMetadata.SourceId] = command.Id;
            }

            var @event = message as Event;
            if(@event != null) {
                envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
                envelope.Metadata[StandardMetadata.SourceId] = @event.Id;
            }

            return envelope;
        }
        
        #region IMessageBus 成员
        /// <summary>
        /// 发布消息
        /// </summary>
        public void Publish(IMessage message)
        {
            _sender.SendAsync(Transform(message));
        }
        /// <summary>
        /// 发布一组消息
        /// </summary>
        public void Publish(IEnumerable<IMessage> messages)
        {
            _sender.SendAsync(messages.Select(Transform));
        }

        #endregion
    }
}
