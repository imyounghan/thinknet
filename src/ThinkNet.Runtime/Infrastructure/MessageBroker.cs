using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ThinkLib.Common;
using ThinkNet.Infrastructure;

namespace ThinkNet.Infrastructure
{
    public class MessageBroker
    {
        class QueueMessage : Message
        {
            public QueueMessage(Message message)
            {
                this.Body = message.Body;
                this.CreatedTime = message.CreatedTime;
                this.MetadataInfo = message.MetadataInfo;
                this.RoutingKey = message.RoutingKey;
            }

            public long Offset { get; set; }

            /// <summary>
            /// 队列id
            /// </summary>
            public int QueueId { get; internal set; }
        }
        class MessageQueue
        {
            private readonly ConcurrentQueue<QueueMessage> queue;
            private readonly int queueId;

            public MessageQueue(int queueId)
            {
                this.queue = new ConcurrentQueue<QueueMessage>();
                this.queueId = queueId;
            }

            public int Id
            {
                get { return this.queueId; }
            }


            /// <summary>
            /// 将消息进队列。
            /// </summary>
            public void Enqueue(QueueMessage message)
            {
                message.QueueId = this.queueId;
                queue.Enqueue(message);
            }
            /// <summary>
            /// 取出消息。
            /// </summary>
            public QueueMessage Dequeue()
            {
                return queue.Dequeue();
            }

            /// <summary>
            /// 队列里的消息数量。
            /// </summary>
            public int Count { get { return queue.Count; } }

            public static MessageQueue[] CreateGroup(int count)
            {
                Ensure.Positive(count, "count");

                MessageQueue[] queues = new MessageQueue[count];
                for (int i = 0; i < count; i++) {
                    queues[i] = new MessageQueue(i);
                }

                return queues;
            }
        }


        private readonly EventWaitHandle waiter;
        private readonly MessageQueue[] queues;
        private readonly ConcurrentDictionary<long, int> offsetDict;
        private readonly ConcurrentDictionary<string, long> topicOffset;
        private int lastQueueIndex;
        private long offset;

        public MessageBroker()
        {
            this.queues = MessageQueue.CreateGroup(4);
            this.waiter = new AutoResetEvent(false);
            this.offsetDict = new ConcurrentDictionary<long, int>();
            this.topicOffset = new ConcurrentDictionary<string, long>(StringComparer.CurrentCultureIgnoreCase);
            this.lastQueueIndex = -1;
        }

        public bool TryAdd(Message message)
        {
            MessageQueue queue;
            if (queues.Length == 1) {
                queue = queues[0];
            }
            else {
                if (string.IsNullOrWhiteSpace(message.RoutingKey)) {
                    queue = queues.OrderBy(p => p.Count).First();
                }
                else {
                    var queueIndex = Math.Abs(message.RoutingKey.GetHashCode() % queues.Length);
                    queue = queues[queueIndex];
                }
            }


            var queueMsg = new QueueMessage(message);


            queueMsg.Offset = topicOffset.AddOrUpdate(message.MetadataInfo[StandardMetadata.Kind], 0,
                            (topic, offset) => Interlocked.Increment(ref offset));

            bool isEmpty = offsetDict.IsEmpty;
            if (!offsetDict.TryAdd(queueMsg.Offset, queue.Id)) {
                return false;
            }
            queue.Enqueue(queueMsg);
            if (isEmpty) {
                waiter.Set();
            }
            return true;
        }

        public bool TryTake(out Message message)
        {
            int queueIndex;
            if (!offsetDict.TryGetValue(offset, out queueIndex) || lastQueueIndex == queueIndex) {
                message = null;
                return false;
            }

            if (offsetDict.TryRemove(offset, out queueIndex)) {
                while (true) {
                    var queueMsg = queues[queueIndex].Dequeue();
                    if (queueMsg.Offset == offset) {
                        message = queueMsg as Message;
                        break;
                    }
                    else {
                        queues[queueIndex].Enqueue(queueMsg);
                    }
                }

                Interlocked.Exchange(ref lastQueueIndex, queueIndex);
                Interlocked.Increment(ref offset);
                
                return true;
            }            

            message = null;
            return false;
        }

        public Message Take()
        {
            Message message;
            if (!this.TryTake(out message)) {
                waiter.WaitOne();
            }

            return message;
        }

        public void Complete(Message message)
        {
            var queueMsg = message as QueueMessage;
            if (queueMsg == null)
                return;

            int completeQueueIndex = queueMsg.QueueId;
            if (Interlocked.CompareExchange(ref lastQueueIndex, -1, completeQueueIndex) == completeQueueIndex) {
                waiter.Set();
            }
        }
    }
}
