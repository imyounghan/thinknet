﻿using System;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示一个消息处理程序的拦截器的接口
    /// </summary>
    public interface IHandlerFilter<TMessage> where TMessage : class, IMessage
    {
        /// <summary>
        /// 在处理消息之前调用
        /// </summary>
        void OnHandlerExecuting(TMessage message);
        /// <summary>
        /// 在处理消息之后调用
        /// </summary>
        void OnHandlerExecuted(TMessage message, Exception exception);
    }
}
