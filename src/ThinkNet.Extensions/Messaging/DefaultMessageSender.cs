using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    public class DefaultMessageSender : IMessageSender
    {
        //private long            offset = 0;

        //private readonly ConcurrentDictionary<MetaMessage.MessageTopic, long> topicOffset;

        private readonly IMessageBroker broker;
        private readonly IMessageStore store;
        public DefaultMessageSender()
        {
        }


        public void Send(MetaMessage message)
        {
            //message.Offset = topicOffset.AddOrUpdate(message.Topic, 1, 
            //    (topic, offset) => Interlocked.Increment(ref offset));

            broker.TryAdd(message);
        }

        public void Send(IEnumerable<MetaMessage> messages)
        {
            messages.ForEach(Send);
        }
    }
}
