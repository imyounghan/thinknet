using System.Collections.Generic;

namespace ThinkNet.Messaging
{
    public interface IMessageSender
    {
        void Send(MetaMessage message);

        void Send(IEnumerable<MetaMessage> messages);
    }
}
