using System;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示一个要发送的信件
    /// </summary>
    public class Envelope
    {
        /// <summary>
        /// 元数据
        /// </summary>
        public class Metadata
        {
            public string Data { get; set; }

            public string TypeName { get; set; }

            public string Namespace { get; set; }

            public string AssemblyName { get; set; }
        }

        public Metadata Body { get; set; }

        public string CorrelationId { get; set; }

        public string RoutingKey { get; set; }

        public Type GetMetadataType()
        {
            var typeFullName = string.Format("{0}.{1}, {2}", Body.Namespace, Body.TypeName, Body.AssemblyName);
            return Type.GetType(typeFullName, true);
        }

        public string Kind { get; set; }

        /// <summary>
        /// 从入队到出队的时间
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 等待入队的时间
        /// </summary>
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// 处理该消息的时长
        /// </summary>
        public TimeSpan ProcessTime { get; set; }
    }
}
