using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class DefaultMessageSender : IMessageSender
    {
        private readonly IMessageBroker messageBroker;
        public DefaultMessageSender(IMessageBroker messageBroker)
        {
            this.messageBroker = messageBroker;
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
                messages.ForEach(message => messageBroker.TryAdd(message));
                successCallback();
            }
            catch (Exception ex) {
                failCallback(ex);
            }
            finally {
                messageBroker.Complete(null);
            }
        }
    }
}
