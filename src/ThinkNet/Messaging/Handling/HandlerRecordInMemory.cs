using System;
using System.Collections.Generic;
using ThinkLib.Common;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 将已完成的处理程序信息记录在内存中。
    /// </summary>
    public class HandlerRecordInMemory : IHandlerRecordStore, IInitializer
    {
        private readonly HashSet<HandlerRecordData> _handlerInfoSet = new HashSet<HandlerRecordData>();


        /// <summary>
        /// 移除超出期限的信息
        /// </summary>
        protected void RemoveHandleInfo()
        {
            _handlerInfoSet.RemoveWhere(item => item.Timestamp.AddHours(1) < DateTime.UtcNow);
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public virtual void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            this.AddHandlerInfo(messageId, messageType.FullName, handlerType.FullName);
        }

        protected void AddHandlerInfo(string messageId, string messageTypeName, string handlerTypeName)
        {
            var messageTypeCode = messageTypeName.GetHashCode();
            var handlerTypeCode = handlerTypeName.GetHashCode();

            _handlerInfoSet.Add(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode));
        }

        public virtual bool HandlerIsExecuted(string messageId, Type messageType, Type handlerType)
        {
            return this.HandlerIsExecuted(messageId, messageType.FullName, handlerType.FullName);
        }

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

        #region IInitializer 成员

        protected virtual void Initialize()
        {        }


        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            this.Initialize();
        }

        #endregion
    }
}
