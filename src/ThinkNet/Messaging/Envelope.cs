
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;

    /// <summary>
    /// 提供封装一个对象的信封
    /// </summary>
    public class Envelope<T> : EventArgs
    {
        public Envelope()
        {
            this.Items = new Hashtable();
        }

        /// <summary>
        /// 初始化一个 <see cref="Envelope{T}"/> 类的新实例
        /// </summary>
        public Envelope(T body)
            : this(body, (string)null, (string)null)
        {
        }

        public Envelope(T body, string messageId)
            : this(body, messageId, (string)null)
        {
        }

        public Envelope(T body, string messageId, string correlationId)
            : this()
        {
            this.Body = body;
            this.MessageId = messageId;
            this.CorrelationId = correlationId;
        }

        /// <summary>
        /// 获取该信封的主体对象
        /// </summary>
        public T Body { get; private set; }

        /// <summary>
        /// 获取相关联的Id
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// 获取消息的Id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// 键值对的集合
        /// </summary>
        public IDictionary Items { get; set; }

        public override string ToString()
        {
            return string.Concat("Envelope{", typeof(T).Name, "}(", this.Body, ")");
        }
    }
}
