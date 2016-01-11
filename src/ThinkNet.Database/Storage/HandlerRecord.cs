using System;
using System.Linq;

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
        public HandlerRecord(string messageId, string messageType, string handlerType)
        {
            this.MessageId = messageId;
            this.HandlerTypeCode = handlerType.GetHashCode();
            this.MessageTypeCode = messageType.GetHashCode();
            this.HandlerType = handlerType;
            this.MessageType = messageType;
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
        public string MessageType { get; set; }
        /// <summary>
        /// 处理器类型名称
        /// </summary>
        public string HandlerType { get; set; }

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
                MessageTypeCode.GetHashCode(),
                HandlerTypeCode.GetHashCode()
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="HandlerRecordData"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as HandlerRecord;
            if (obj == null) {
                return false;
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
            return string.Format("{0}_{1}_{2}", MessageId, MessageTypeCode, HandlerTypeCode);
        }
    }
}
