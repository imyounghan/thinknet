using System.Collections.Generic;
using ThinkNet.Common;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    [RequiredComponent(typeof(DefaultMessageSender))]
    public interface IMessageSender
    {
        void Send(Message message);

        void Send(IEnumerable<Message> messages);
    }


    internal class DefaultMessageSender : IMessageSender
    {
        private readonly IMessageBroker broker;
        public DefaultMessageSender(IMessageBroker messageBroker)
        {
            this.broker = messageBroker;
        }


        public void Send(Message message)
        {
            this.Send(new[] { message });
        }

        public void Send(IEnumerable<Message> messages)
        {
            messages.ForEach(message => broker.TryAdd(message));
        }
    }
}
