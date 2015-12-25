using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThinkNet.Messaging.Queuing
{
    public class DefaultMessageQueueFactory : IMessageQueueFactory
    {
        class MessageQueue : IMessageQueue
        {
            private readonly ConcurrentQueue<MetaMessage> _queue = new ConcurrentQueue<MetaMessage>();
            private int _running = 1;


            public void Enqueue(MetaMessage message)
            {
                _queue.Enqueue(message);
            }

            public MetaMessage Dequeue()
            {
                MetaMessage message = null;
                if (!_queue.IsEmpty && Interlocked.CompareExchange(ref _running, 0, 1) == 1 && _queue.TryDequeue(out message)) {
                    return message;
                }
                return null;
            }

            public bool Ack()
            {
                return Interlocked.CompareExchange(ref _running, 1, 0) == 0;
            }

            public int Count
            {
                get { return _queue.Count; }
            }
        }

        public IMessageQueue Create()
        {
            return new MessageQueue();
        }

        public IMessageQueue[] CreateGroup(int count)
        {
            Ensure.Positive(count, "count");

            IMessageQueue[] queues = new IMessageQueue[count];
            for (int i = 0; i < count; i++) {
                queues[i] = new MessageQueue();
            }

            return queues;
        }
    }
}
