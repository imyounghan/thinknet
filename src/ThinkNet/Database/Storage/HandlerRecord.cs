using System;
using System.Linq;
using ThinkLib;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 处理程序信息
    /// </summary>
    [Serializable]
    public class HandlerRecord
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HandlerRecord()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public HandlerRecord(string messageId, Type messageType, Type handlerType)
        {
            this.MessageId = messageId;
            this.HandlerTypeCode = handlerType.FullName.GetHashCode();
            this.MessageTypeCode = messageType.FullName.GetHashCode();
            this.HandlerTypeName = handlerType.GetFullName();
            this.MessageTypeName = messageType.GetFullName();
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 相关id
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// 消息类型编码
        /// </summary>
        public int MessageTypeCode { get; set; }
        /// <summary>
        /// 处理器类型编码
        /// </summary>
        public int HandlerTypeCode { get; set; }

        /// <summary>
        /// 消息类型名称
        /// </summary>
        public string MessageTypeName { get; set; }
        /// <summary>
        /// 处理器类型名称
        /// </summary>
        public string HandlerTypeName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return new int[] {
                MessageId.GetHashCode(),
                MessageTypeCode,
                HandlerTypeCode
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="HandlerRecord"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as HandlerRecord;
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }

            return other.MessageId == this.MessageId
                && other.HandlerTypeCode == this.HandlerTypeCode
                && other.MessageTypeCode == this.MessageTypeCode;
        }

        /// <summary>
        /// 将此实例的标识转换为其等效的字符串表示形式。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}&{2}", MessageId, MessageTypeName, HandlerTypeName);
        }
    }
}
