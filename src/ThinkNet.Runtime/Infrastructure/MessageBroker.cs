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

        private int lastQueueIndex;
        private long consumeOffset;
        private long produceOffset;

        public MessageBroker()
        {
            this.queues = MessageQueue.CreateGroup(4);
            this.waiter = new EventWaitHandle(true, EventResetMode.AutoReset);
            this.offsetDict = new ConcurrentDictionary<long, int>();
            this.lastQueueIndex = -1;
            this.produceOffset = -1;
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

            return true;
        }

        public bool TryTake(out Message message)
        {
            int queueIndex;
            if (!offsetDict.TryGetValue(consumeOffset, out queueIndex) || lastQueueIndex == queueIndex) {
                message = null;
                return false;
            }

            if (offsetDict.TryRemove(consumeOffset, out queueIndex)) {
                //while (true) {
                //    var queueMsg = queues[queueIndex].Dequeue();
                //    if (queueMsg.Offset == consumeOffset) {
                //        message = queueMsg as Message;
                //        break;
                //    }
                //    else {
                //        queues[queueIndex].Enqueue(queueMsg);
                //    }
                //}
                message = queues[queueIndex].Dequeue();

                Interlocked.Exchange(ref lastQueueIndex, queueIndex);
                Interlocked.Increment(ref consumeOffset);
                
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

        public void Complete(Message message = null)
        {
            var queueMsg = message as QueueMessage;

            bool release;
            if (queueMsg != null) {
                int completeQueueIndex = queueMsg.QueueId;
                release = Interlocked.CompareExchange(ref lastQueueIndex, -1, completeQueueIndex) == completeQueueIndex;
            }
            else {
                release = lastQueueIndex == -1;
            }

            if (release) {
                waiter.Set();
            }
        }
    }
}
