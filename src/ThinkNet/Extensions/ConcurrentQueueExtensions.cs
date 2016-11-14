using System.Collections.Concurrent;

namespace ThinkNet
{
    /// <summary>
    /// ConcurrentQueue 的扩展类
    /// </summary>
    public static class ConcurrentQueueExtensions
    {
        /// <summary>
        /// 移除并返回位于队列开始处的元素。
        /// </summary>
        /// <returns>如果成功移除返回队列开始处的元素，否则返回该泛型的默认值。</returns>
        public static T Dequeue<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            if (queue.TryDequeue(out item)) {
                return item;
            }

            return default(T);
        }
    }
}
