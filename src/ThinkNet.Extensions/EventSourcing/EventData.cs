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
        public EventData(int version, string payload, string correlationId)
            : this()
        {
            this.Payload = payload;
            this.CorrelationId = correlationId;
        }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }     
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
    }
}
