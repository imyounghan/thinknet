using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ThinkNet.Infrastructure
{
    public class ParallelQueue<T>
    {
        public class Message<T>
        {
            IDictionary<string, string> MetadataInfo { get; set; }

            public string Id { get; set; }

            public long SequenceNumber { get; set; }

            public string RoutingKey { get; set; }

            public int QueueId { get; internal set; }

            T Body { get; set; }
        }

        class MessageQueue
        {
            private readonly ConcurrentQueue<Message<T>> queue;
            private int running = 1;
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
                if (queue.IsEmpty) {
                    Interlocked.Increment(ref runtimes);
                }

                message.QueueId = this.queueId;
                queue.Enqueue(message);
            }
            /// <summary>
            /// 取出消息。
            /// </summary>
            public Message<T> Dequeue()
            {
                Message<T> message = null;
                if (!queue.IsEmpty && Interlocked.CompareExchange(ref running, 0, 1) == 1) {
                    queue.TryDequeue(out message);
                    Interlocked.Decrement(ref runtimes);

                }
                return message;
            }

            public void Ack()
            {
                if (Interlocked.CompareExchange(ref running, 1, 0) == 0 && !queue.IsEmpty) {
                    Interlocked.Increment(ref runtimes);
                }
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
        private readonly int queueCount;
        private int index;
        private static long runtimes = 0;

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
                    var queueIndex = message.RoutingKey.GetHashCode() % queueCount;
                    if (queueIndex < 0) {
                        queueIndex = Math.Abs(queueIndex);
                    }
                    queue = queues[queueIndex];
                }
            }

            if (queue.Count >= 1000) {
                return false;
            }

            queue.Enqueue(message);
            if (Interlocked.Read(ref runtimes) > 0) {
                waiter.Set();
            }
            return true;
        }

        public bool TryTake(out Message<T> message)
        {
            var queueIndex = Interlocked.Increment(ref index) % queueCount;
            message = queues[queueIndex].Dequeue();

            return message != null;
        }

        public Message<T> Take()
        {
            if (Interlocked.Read(ref runtimes) == 0) {
                waiter.WaitOne();
            }

            Message<T> message;
            while (!this.TryTake(out message)) {
                return message;
            }

            return null;
        }

        public void Complete(Message<T> message)
        {
            queues[message.QueueId].Ack();
            if (Interlocked.Read(ref runtimes) > 0) {
                waiter.Set();
            }
        }
    }
}
