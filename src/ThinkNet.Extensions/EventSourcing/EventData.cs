using System;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 历史事件(用于还原溯源聚合的事件)
    /// </summary>
    public class EventData
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventData()
        {
            this.CreatedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventData(string sourceId, string sourceType, int version, string payload, string correlationId)
            : this()
        {
            this.SourceKey = new ComplexKey(sourceId, sourceType, version);
            this.Payload = payload;
            this.CorrelationId = correlationId;
        }

        /// <summary>
        /// Key。
        /// </summary>
        public ComplexKey SourceKey { get; set; }        
        /// <summary>
        /// 事件流
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// 发布事件的相关id
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 生成事件的时间戳
        /// </summary>
        public DateTime CreatedOn { get; set; }


        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string TypeName { get; set; }
    }
}
