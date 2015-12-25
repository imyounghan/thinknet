
using System.Collections.Concurrent;
using ThinkNet.Infrastructure;
namespace ThinkNet.Messaging.Queuing
{
    /// <summary>
    /// 表示这是一个消息队列。
    /// </summary>
    public class MessageQueue
    {
        /// <summary>
        /// 取出消息。
        /// </summary>
        MetaMessage Take();
        private readonly BlockingCollection<MetaMessage> queue;
        private long            offset = 0;
        private int queueId;

        public MessageQueue()
        {
            this.queue = new BlockingCollection<MetaMessage>();
        }


        /// <summary>
        /// 将消息进队列。
        /// </summary>
        public void Enqueue(MetaMessage message)
        {
            message.QueueId = queueId;
            if (message.Offset == 0) {
            }
            queue.TryAdd(message);
        }
        /// <summary>
        /// 取出消息。
        /// </summary>
        public MetaMessage Dequeue()
        {
            MetaMessage message = null;
            if (!_queue.IsEmpty && Interlocked.CompareExchange(ref _running, 0, 1) == 1
                && _queue.TryDequeue(out message)) {
                return message;
            }
            return null;
        }
        /// <summary>
        /// 队列里的消息数量。
        /// </summary>
        public int Count { get { return _queue.Count; } }

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
