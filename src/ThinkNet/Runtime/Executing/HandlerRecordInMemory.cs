using System;
using System.Collections.Generic;
using System.Threading;

namespace ThinkNet.Runtime.Executing
{
    /// <summary>
    /// 将已完成的处理程序信息记录在内存中。
    /// </summary>
    public class HandlerRecordInMemory : IHandlerRecordStore
    {
        private readonly HashSet<HandlerRecordData> _handlerInfoSet;
        private readonly Timer timer;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public HandlerRecordInMemory()
        {
            timer = new Timer(RemoveHandleInfo, null, 5000, 2000);
            _handlerInfoSet = new HashSet<HandlerRecordData>();
        }

        /// <summary>
        /// 移除超出期限的信息
        /// </summary>
        protected void RemoveHandleInfo(object state)
        {
            _handlerInfoSet.RemoveWhere(item => item.Timestamp.AddMinutes(1) < DateTime.UtcNow);
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public virtual void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            this.AddHandlerInfo(messageId, messageType.FullName, handlerType.FullName);
        }

        /// <summary>
        /// 添加处理程序信息到内存中
        /// </summary>
        protected void AddHandlerInfo(string messageId, string messageTypeName, string handlerTypeName)
        {
            var messageTypeCode = messageTypeName.GetHashCode();
            var handlerTypeCode = handlerTypeName.GetHashCode();

            _handlerInfoSet.Add(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode));
        }
        /// <summary>
        /// 一个表示该处理程序信息是否执行过的返回值 。
        /// </summary>
        public virtual bool HandlerIsExecuted(string messageId, Type messageType, Type handlerType)
        {
            return this.HandlerIsExecuted(messageId, messageType.FullName, handlerType.FullName);
        }
        /// <summary>
        /// 判断内存中是否存在该处理程序信息。
        /// </summary>
        protected bool HandlerIsExecuted(string messageId, string messageTypeName, string handlerTypeName)
        {
            var messageTypeCode = messageTypeName.GetHashCode();
            var handlerTypeCode = handlerTypeName.GetHashCode();

            return _handlerInfoSet.Contains(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode));
        }


        class HandlerRecordData
        {
            /// <summary>
            /// Parameterized constructor.
            /// </summary>
            public HandlerRecordData(string messageId, int messageTypeCode, int handlerTypeCode)
            {
                this.MessageId = messageId;
                this.HandlerTypeCode = handlerTypeCode;
                this.MessageTypeCode = messageTypeCode;
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

            public bool Finished { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// 返回此实例的哈希代码
            /// </summary>
            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            /// <summary>
            /// 确定此实例是否与指定的对象（也必须是 <see cref="HandlerRecordData"/> 对象）相同。
            /// </summary>
            public override bool Equals(object obj)
            {
                var other = obj as HandlerRecordData;           
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
                return string.Format("{0}_{1}_{2}", MessageId, MessageTypeCode, HandlerTypeCode);
            }
        }
    }
}
