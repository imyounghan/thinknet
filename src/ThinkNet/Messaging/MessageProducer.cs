
namespace ThinkNet.Messaging
{
    using System.Collections.Generic;

    using ThinkNet.Infrastructure;

    public class MessageProducer<TMessage> : MessageBroker<TMessage>, IMessageBus<TMessage>
        where TMessage : IMessage
    {
        public MessageProducer(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }

        #region IMessageBus<TMessage> 成员

        public void Send(TMessage message)
        {
            this.Send(new Envelope<TMessage>(message) { MessageId = ObjectId.GenerateNewStringId() });
        }

        public void Send(Envelope<TMessage> message)
        {
            this.Append(message);
        }

        public void Send(IEnumerable<TMessage> messages)
        {
            if(messages == null) {
                return;
            }

            messages.ForEach(this.Send);
        }

        public void Send(IEnumerable<Envelope<TMessage>> messages)
        {
            if(messages == null) {
                return;
            }

            messages.ForEach(this.Send);
        }

        #endregion
    }
}
