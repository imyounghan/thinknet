using System;

namespace ThinkNet.Common
{
    /// <summary>
    /// Provides the envelope for an object that will be sent to a buffer.
    /// </summary>
    public class Envelope<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope{T}"/> class.
        /// </summary>
        public Envelope(T body)
        {
            this.Body = body;
        }

        /// <summary>
        /// Gets the body.
        /// </summary>
        public T Body { get; set; }

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
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
