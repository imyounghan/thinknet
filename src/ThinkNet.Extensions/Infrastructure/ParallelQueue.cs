using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示这个并行的消息队列
    /// </summary>
    public class ParallelQueue<T>
    {
        /// <summary>
        /// 队列消息
        /// </summary>
        public class Message<T>
        {
            /// <summary>
            /// 元数据信息
            /// </summary>
            IDictionary<string, string> MetadataInfo { get; set; }

            /// <summary>
            /// id
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// offset
            /// </summary>
            public long Offset { get; set; }

            /// <summary>
            /// 用于路由的值
            /// </summary>
            public string RoutingKey { get; set; }

            /// <summary>
            /// 队列id
            /// </summary>
            public int QueueId { get; internal set; }

            /// <summary>
            /// 数据
            /// </summary>
            T Body { get; set; }
        }

        class MessageQueue
        {
            private readonly ConcurrentQueue<Message<T>> queue;
            private readonly int queueId;

            public MessageQueue(int queueId)
            {
                this.queue = new ConcurrentQueue<Message<T>>();
                this.queueId = queueId;
            }

            public int Id
            {
                get { return this.queueId; }
            }


            /// <summary>
            /// 将消息进队列。
            /// </summary>
            public void Enqueue(Message<T> message)
            {
                message.QueueId = this.queueId;
                queue.Enqueue(message);
            }
            /// <summary>
            /// 取出消息。
            /// </summary>
            public Message<T> Dequeue()
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
        private long offset;

        public ParallelQueue(int queueCount)
        {
            this.queueCount = queueCount;
            this.queues = MessageQueue.CreateGroup(queueCount);
            this.index = 0;
            this.waiter = new AutoResetEvent(false);
        }

        public bool TryAdd(Message<T> message)
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


            bool isEmpty = offsetDict.IsEmpty;
            if (!offsetDict.TryAdd(message.Offset, queue.Id)) {
                return false;
            }
            queue.Enqueue(message);
            if (isEmpty) {
                waiter.Set();
            }
            return true;
        }

        public bool TryTake(out Message<T> message)
        {
            int queueIndex;
            if (!offsetDict.TryGetValue(offset, out queueIndex) || lastQueueIndex == queueIndex) {
                message = null;
                return false;
            }

            if (offsetDict.TryRemove(offset, out queueIndex)) {
                Interlocked.Exchange(ref lastQueueIndex, queueIndex);
                Interlocked.Increment(ref offset);
                
                message = queues[queueIndex].Dequeue();
                return true;
            }            

            message = null;
            return false;
        }

        public Message<T> Take()
        {
            Message<T> message;
            if (!this.TryTake(out message)) {
                waiter.WaitOne();
            }

            return message;
        }

        public void Complete(Message<T> message)
        {
            int completeQueueIndex = message.QueueId;
            if (Interlocked.CompareExchange(ref lastQueueIndex, -1, completeQueueIndex) == completeQueueIndex) {
                waiter.Set();
            }
        }
    }
}
