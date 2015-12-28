using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThinkNet.Infrastructure
{
    public class ParallelQueue<T>
    {
        class MessageQueue
        {
            private readonly ConcurrentQueue<T> queue;
            private int running = 1;
            private int _queueId;

            public MessageQueue()
            {
                this.queue = new ConcurrentQueue<T>();
            }

            public int Id
            {
                get;
            }


            /// <summary>
            /// 将消息进队列。
            /// </summary>
            public void Enqueue(T message)
            {
                queue.Enqueue(message);

                if (queue.IsEmpty) {
                    //Interlocked.Increment(ref runtimes);
                }
            }
            /// <summary>
            /// 取出消息。
            /// </summary>
            public T Dequeue()
            {
                T message = default(T);
                if (!queue.IsEmpty && Interlocked.CompareExchange(ref running, 0, 1) == 1) {
                    queue.TryDequeue(out message);
                    //Interlocked.Decrement(ref runtimes);

                }
                return message;
            }

            public void Ack()
            {
                if (Interlocked.CompareExchange(ref running, 1, 0) == 0) {
                    //Interlocked.Increment(ref runtimes);
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
                    queues[i] = new MessageQueue();
                }

                return queues;
            }
        }
    }
}
