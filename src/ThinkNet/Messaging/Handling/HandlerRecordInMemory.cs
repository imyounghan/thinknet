using System;
using System.Collections.Generic;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 将已完成的处理程序信息记录在内存中。
    /// </summary>
    public class HandlerRecordInMemory : IHandlerRecordStore
    {
        private readonly HashSet<HandlerRecordData> _handlerInfoSet = new HashSet<HandlerRecordData>();
        //private readonly ITypeCodeProvider _typeCodeProvider;

        ///// <summary>
        ///// Default Constructor.
        ///// </summary>
        //public HandlerRecordInMemory(ITypeCodeProvider typeCodeProvider)
        //{
        //    this._handlerInfoSet = new HashSet<HandlerRecordData>();
        //    this._typeCodeProvider = typeCodeProvider;
        //}

        /// <summary>
        /// 移除超出期限的信息
        /// </summary>
        protected void RemoveHandleInfo()
        {
            _handlerInfoSet.RemoveWhere(item => item.Timestamp.AddHours(1) < DateTime.Now);
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public virtual void AddHandlerInfo(string messageId, string messageType, string handlerType)
        {
            var messageTypeCode = messageType.GetHashCode();// _typeCodeProvider.GetTypeCode(messageType);
            var handlerTypeCode = handlerType.GetHashCode();// _typeCodeProvider.GetTypeCode(handlerType);

            this.AddHandlerInfoToMemory(messageId, messageTypeCode, handlerTypeCode);
        }


        private void AddHandlerInfoToMemory(string messageId, int messageTypeCode, int handlerTypeCode)
        {
            _handlerInfoSet.Add(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode));
        }

        

        /// <summary>
        /// 检查该处理程序信息是否存在
        /// </summary>
        public bool IsHandlerInfoExist(string messageId, string messageType, string handlerType)
        {
            var messageTypeCode = messageType.GetHashCode();// _typeCodeProvider.GetTypeCode(messageType);
            var handlerTypeCode = handlerType.GetHashCode();// _typeCodeProvider.GetTypeCode(handlerType);

            if (_handlerInfoSet.Contains(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode))) {
                return true;
            }

            var existence = this.IsHandlerInfoExist(messageId, messageType, handlerType);
            if (existence) {
                this.AddHandlerInfoToMemory(messageId, messageTypeCode, handlerTypeCode);
            }

            return existence;
        }

        /// <summary>
        /// 检查该处理程序信息是否存在
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckHandlerInfoExist(string messageId, string messageType, string handlerType)
        {
            return false;
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
