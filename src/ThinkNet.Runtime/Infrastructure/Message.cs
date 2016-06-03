using System;
using System.Collections.Generic;

namespace ThinkNet.Infrastructure
{
    public class Message<T>
    {
        public Message()
        {
            this.CreatedTime = DateTime.UtcNow;
        }

        ///// <summary>
        ///// 元数据信息
        ///// </summary>
        //public IDictionary<string, string> MetadataInfo { get; set; }

        /// <summary>
        /// 用于路由的值
        /// </summary>
        public string RoutingKey { get; set; }
        
        public DateTime CreatedTime { get; set; }

        public TimeSpan TimeForWait { get; set; }

        public T Body { get; set; }
    }
}
