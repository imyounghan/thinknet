using System;
using System.Collections.Generic;

namespace ThinkNet.Messaging
{
    public class Message
    {
        /// <summary>
        /// 元数据信息
        /// </summary>
        public IDictionary<string, string> MetadataInfo { get; set; }

        /// <summary>
        /// 用于路由的值
        /// </summary>
        public string RoutingKey { get; set; }
        
        public DateTime CreatedTime { get; set; }

        public object Body { get; set; }
    }
}
