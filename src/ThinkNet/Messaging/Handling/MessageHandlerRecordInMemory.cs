using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThinkNet.Infrastructure;
using ThinkNet.Runtime;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 将已完成的处理程序信息记录在内存中。
    /// </summary>
    public class MessageHandlerRecordInMemory : IMessageHandlerRecordStore, IInitializer, IProcessor
    {
        private readonly HashSet<HandlerRecordData> _handlerInfoSet;
        //private readonly TimeScheduler _scheduler;
        private Timer _scheduler;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MessageHandlerRecordInMemory()
        {
            this._handlerInfoSet = new HashSet<HandlerRecordData>();
            //this._scheduler = TimeScheduler.Create("Recording Handler Scheduler", Planning).SetInterval(2000);
        }

        private void Planning(object state)
        {
            this.RemoveHandleInfo();
            this.TimeProcessing();
        }

        /// <summary>
        /// 定时处理程序
        /// </summary>
        protected virtual void TimeProcessing()
        { }

        /// <summary>
        /// 移除超出期限的信息
        /// </summary>
        private void RemoveHandleInfo()
        {
            if(_handlerInfoSet.Count == 0)
                return;

            var tick = DateTime.UtcNow.AddMinutes(-1);
            _handlerInfoSet.RemoveWhere(item => !item.IsNull() && item.Timestamp < tick);
        }

        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        public virtual void AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        { }

        /// <summary>
        /// 添加处理程序信息到内存中
        /// </summary>
        protected void AddHandlerInfoToMemory(string messageId, string messageTypeName, string handlerTypeName)
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
        private bool HandlerIsExecuted(string messageId, string messageTypeName, string handlerTypeName)
        {
            var messageTypeCode = messageTypeName.GetHashCode();
            var handlerTypeCode = handlerTypeName.GetHashCode();

            return _handlerInfoSet.Contains(new HandlerRecordData(messageId, messageTypeCode, handlerTypeCode));
        }

        void IMessageHandlerRecordStore.AddHandlerInfo(string messageId, Type messageType, Type handlerType)
        {
            this.AddHandlerInfoToMemory(messageId, messageType.FullName, handlerType.FullName);
            this.AddHandlerInfo(messageId, messageType, handlerType);
        }

        bool IMessageHandlerRecordStore.HandlerIsExecuted(string messageId, Type messageType, Type handlerType)
        {
            return this.HandlerIsExecuted(messageId, messageType.FullName, handlerType.FullName) ||
                this.HandlerIsExecuted(messageId, messageType, handlerType);
        }

        void IInitializer.Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            container.RegisterInstance<IProcessor>(this, "recordhandler");
        }

        void IProcessor.Start()
        {
            if(_scheduler == null) {
                _scheduler = new Timer(Planning, null, 5000, 2000);
            }
            //_scheduler.Start();
        }

        void IProcessor.Stop()
        {
            if(_scheduler != null) {
                _scheduler.Dispose();
                _scheduler = null;
            }
            //_scheduler.Stop();
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
                return new int[] {
                    MessageId.GetHashCode(),
                    HandlerTypeCode.GetHashCode(),
                    MessageTypeCode.GetHashCode()
                }.Aggregate((x, y) => x ^ y);
            }

            /// <summary>
            /// 确定此实例是否与指定的对象（也必须是 <see cref="HandlerRecordData"/> 对象）相同。
            /// </summary>
            public override bool Equals(object obj)
            {
                if(obj == null)
                    return false;

                var other = (HandlerRecordData)obj;

                return other.MessageId == this.MessageId
                    && other.HandlerTypeCode == this.HandlerTypeCode
                    && other.MessageTypeCode == this.MessageTypeCode;
            }

            ///// <summary>
            ///// 将此实例的标识转换为其等效的字符串表示形式。
            ///// </summary>
            //public override string ToString()
            //{
            //    return string.Format("{0}_{1}_{2}", MessageId, MessageTypeCode, HandlerTypeCode);
            //}
        }
    }
}
