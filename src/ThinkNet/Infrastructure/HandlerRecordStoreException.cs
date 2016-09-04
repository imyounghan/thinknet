using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示存储处理器信息的异常类
    /// </summary>
    public class HandlerRecordStoreException : ThinkNetException
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public HandlerRecordStoreException(string message)
            : base(message)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public HandlerRecordStoreException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
