﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示一个存在多个消息处理程序的异常
    /// </summary>
    public class MessageHandlerTooManyException : ThinkNetException
    {
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MessageHandlerTooManyException(Type type)
            : base(string.Format("Found more than one message handler for {0}.", type.FullName)) 
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MessageHandlerTooManyException(IEnumerable<Type> types)
           : base(string.Format("Found more than one event handler form '{0}'.", string.Join(",", types.Select(p => p.FullName).ToArray())))
        { }
    }
}
