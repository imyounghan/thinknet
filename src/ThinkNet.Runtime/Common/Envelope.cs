using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkNet.Infrastructure
{
    ///// <summary>
    ///// Static factory class for <see cref="Envelope{T}"/>.
    ///// </summary>
    //public abstract class Envelope
    //{
    //    //public interface IMetadata
    //    //{
    //    //    string Topic { get; }

    //    //    long Offset { get; }

    //    //    int QueueId { get; }

    //    //    /////// <summary>
    //    //    /////// Gets the correlation id.
    //    //    /////// </summary>
    //    //    ////string CorrelationId { get; }

    //    //    /// <summary>
    //    //    /// Gets the correlation id.
    //    //    /// </summary>
    //    //    string MessageId { get; }
    //    //}

    //    /// <summary>
    //    /// Creates an envelope for the given body.
    //    /// </summary>
    //    public static Envelope<T> Create<T>(T body)
    //    {
    //        return new Envelope<T>(body);
    //    }
    //}

    /// <summary>
    /// Provides the envelope for an object that will be sent to a bus.
    /// </summary>
    public class Envelope<T>// : Envelope//, Envelope.IMetadata
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
        public T Body { get; private set; }

        /// <summary>
        /// 从入队到出队的时间
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 从消息构造到等待处理的时间
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
