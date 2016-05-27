using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public interface IMessageSender
    {
        void Send(Message message);

        void SendAsync(Message message, Action successCallback, Action<Exception> failCallback);

        void Send(IEnumerable<Message> messages);

        void SendAsync(IEnumerable<Message> messages, Action successCallback, Action<Exception> failCallback);
    }
}
