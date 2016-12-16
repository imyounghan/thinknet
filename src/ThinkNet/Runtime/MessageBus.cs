using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var eventCollection = message as EventCollection;
            if(eventCollection != null) {
                envelope.Metadata[StandardMetadata.SourceId] = eventCollection.SourceId.Id;
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
        public Task PublishAsync(IMessage message)
        {
            return this.PublishAsync(new[] { message });
        }
        /// <summary>
        /// 发布一组消息
        /// </summary>
        public Task PublishAsync(IEnumerable<IMessage> messages)
        {
            if(LogManager.Default.IsDebugEnabled) {
                var stringArray = messages.Select(item => item.ToString());

                LogManager.Default.DebugFormat("Publishing a batch of messages({0}) to local queue.",
                    string.Join(",", stringArray));
            }

            return Task.Factory.StartNew(delegate {
                messages.Select(Transform).ForEach(_sender.Send);
            });
        }

        #endregion
    }
}
