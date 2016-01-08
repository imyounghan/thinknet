using System;
using System.Linq;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 消息数据
    /// </summary>
    [Serializable]
    public class MessageData : IEquatable<MessageData>
    {
        /// <summary>
        /// 消息id
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageType { get; set; }
        /// <summary>
        /// 消息主体
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// 生成消息的时间
        /// </summary>
        public DateTime DeliveryDate { get; set; }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return new int[] {
                MessageId.GetHashCode(),
                MessageType.GetHashCode()
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="MessageData"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj != null && obj is MessageData) {
                return Equals((MessageData)obj);
            }

            return false;
        }

        /// <summary>
        /// 将此实例的标识转换为其等效的字符串表示形式。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}_{1}_{2}", MessageId, MessageType, Body);
        }

        private bool Equals(MessageData other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return other.MessageId == this.MessageId
                && other.MessageType == this.MessageType;
        }

        bool IEquatable<MessageData>.Equals(MessageData other)
        {
            return this.Equals(other);
        }
    }
}
