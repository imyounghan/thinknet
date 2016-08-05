using System;

namespace ThinkNet.Infrastructure
{
    public class Message<T>// : IMessageMetadata
    {
        ///// <summary>
        ///// 用于路由的值
        ///// </summary>
        //public string RoutingKey { get; set; }

        public long Offset { get; set; }

        public int QueueId { get; set; }
        
        //public DateTime CreatedTime { get; set; }

        public TimeSpan TimeForWait { get; set; }

        public T Body { get; set; }
    }
}
