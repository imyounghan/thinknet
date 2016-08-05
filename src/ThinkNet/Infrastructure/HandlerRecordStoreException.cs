using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
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
