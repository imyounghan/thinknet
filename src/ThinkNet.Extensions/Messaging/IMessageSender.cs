using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    public interface IMessageSender
    {
        void Send(MetaMessage message);

        void Send(IEnumerable<MetaMessage> messages);
    }


    public class DefaultMessageSender : IMessageSender
    {
        //private long            offset = 0;

        private readonly ConcurrentDictionary<string, long> topicOffset;

        private readonly IMessageBroker broker;
        private readonly IMessageStore store;
        public DefaultMessageSender()
        {
        }


        public void Send(MetaMessage message)
        {
            this.Send(new[] { message });
        }

        public void Send(IEnumerable<MetaMessage> messages)
        {
            Task.Factory
                .StartNew(() => {
                    messages.ForEach(message => {
                        //message.Id = Guid.NewGuid().ToString();
                        message.Offset = topicOffset.AddOrUpdate(message.Topic, 1,
                            (topic, offset) => Interlocked.Increment(ref offset));
                    });
                    store.Add(messages);
                }, TaskCreationOptions.PreferFairness)
                .ContinueWith(task => {
                    if(task.Status != TaskStatus.RanToCompletion)
                        return;
                    messages.ForEach(message => broker.TryAdd(message));
                }).Wait();
        }
    }
}
