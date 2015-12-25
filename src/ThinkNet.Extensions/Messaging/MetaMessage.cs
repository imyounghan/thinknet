using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Messaging
{
    public class MetaMessage
    {
        public MetaMessage()
        {
            this.CreatedTime = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public long Offset { get; set; }

        public int QueueId { get; set; }
        public DateTime CreatedTime { get; set; }
        //public string Tag { get; set; }
        //public string Topic { get; set; }

        public object Body { get; set; }

        public string RoutingKey { get; set; }
    }
}
