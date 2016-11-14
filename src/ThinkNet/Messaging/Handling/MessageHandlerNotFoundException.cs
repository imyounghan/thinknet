using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示一个当找不到消息处理程序的异常
    /// </summary>
    [Serializable]
    public class MessageHandlerNotFoundException : ThinkNetException
    {
        /// <summary>Parameterized constructor.
        /// </summary>
        public MessageHandlerNotFoundException(Type type)
            : base(string.Format("Message Handler not found for {0}.", type.FullName))
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MessageHandlerNotFoundException(IEnumerable<Type> types)
            : base(string.Format("Event Handler not found for '{0}'.", string.Join(",", types.Select(p => p.FullName).ToArray())))
        { }
    }
}
