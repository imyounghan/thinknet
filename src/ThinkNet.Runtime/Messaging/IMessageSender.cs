using System.Collections.Generic;
using ThinkLib.Common;
using ThinkNet.Infrastructure;

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
        private readonly MessageBroker broker;
        public DefaultMessageSender()
        {
            this.broker = MessageBrokerFactory.Instance.GetOrCreate("message");
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
