using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkLib.Common;

namespace ThinkNet.Messaging
{
    [UnderlyingComponent(typeof(DefaultMessageSender))]
    public interface IMessageSender
    {
        void Send(Message message);

        void SendAsync(Message message, Action successCallback, Action<Exception> failCallback);

        void Send(IEnumerable<Message> messages);

        void SendAsync(IEnumerable<Message> messages, Action successCallback, Action<Exception> failCallback);
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
            this.SendAsync(message, () => { }, (ex) => { });
        }

        public void SendAsync(Message message, Action successCallback, Action<Exception> failCallback)
        {
            this.SendAsync(new[] { message }, successCallback, failCallback);
        }

        public void Send(IEnumerable<Message> messages)
        {
            this.SendAsync(messages, () => { }, (ex) => { });
        }

        public void SendAsync(IEnumerable<Message> messages, Action successCallback, Action<Exception> failCallback)
        {
            try {
                messages.ForEach(message => broker.TryAdd(message));
                successCallback();               
            }
            catch (Exception ex) {
                failCallback(ex);
            }
            finally {
                broker.Complete(null);
            }
        }
    }
}
