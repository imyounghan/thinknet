using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ThinkNet.Configurations;

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

            public override string ToString()
            {
                return string.Concat(Body, "(", QueueId, "-", Offset, ")");
            }
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
                count.MustPositive("count");

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

        private long consumeOffset;
        private long produceOffset;
        private long totalCount;

        public MessageBroker()
        {
            this.queues = MessageQueue.CreateGroup(ConfigurationSetting.Current.QueueCount);
            this.waiter = new AutoResetEvent(false);
            this.offsetDict = new ConcurrentDictionary<long, int>();

            this.produceOffset = -1;
            //this.consumeOffset = -1;
            //System.IO.File.Delete("log.txt");
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


            var queueMsg = new QueueMessage(message) {
                Offset = Interlocked.Increment(ref produceOffset)
            };
            //queueMsg.Offset = topicOffset.AddOrUpdate(message.MetadataInfo[StandardMetadata.Kind], 0,
            //                (topic, offset) => Interlocked.Increment(ref offset));

            if (!offsetDict.TryAdd(produceOffset, queue.Id)) {
                return false;
            }
            queue.Enqueue(queueMsg);
            Interlocked.Increment(ref totalCount);

            return true;
        }

        public bool TryTake(out Message message)
        {
            int currentQueueIndex;
            int previousQueueIndex;
            if (offsetDict.TryGetValue(consumeOffset, out currentQueueIndex) &&
                (!offsetDict.TryGetValue(consumeOffset - 1, out previousQueueIndex) || currentQueueIndex != previousQueueIndex)) {
                
                message = queues[currentQueueIndex].Dequeue();

                var valid = !message.IsNull();

                if (valid) {
                    Interlocked.Increment(ref consumeOffset);
                    Interlocked.Decrement(ref totalCount);
                }

                return valid;
            }
                     

            message = null;
            return false;
        }

        public Message Take()
        {
            Message message;
            while (!this.TryTake(out message)) {
                waiter.WaitOne(100);
            }
            

            return message;
        }

        public void Complete(Message message)
        {
            var queueMessage = message as QueueMessage;
            if (message.IsNull() || queueMessage.IsNull()) {
                return;
            }

            offsetDict.Remove(queueMessage.Offset);
            waiter.Set();
        }
    }
}
