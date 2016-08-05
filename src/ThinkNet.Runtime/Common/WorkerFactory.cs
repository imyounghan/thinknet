using System;

namespace ThinkNet.Common
{
    /// <summary>
    /// 创建后台线程工作器工厂类
    /// </summary>
    public static class WorkerFactory
    {
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker Create(Action action)
        {
            return new Worker(action, null, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker Create(Action action, Action successCallback)
        {
            return new Worker(action, successCallback, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker Create(Action action, Action<Exception> exceptionCallback)
        {
            return new Worker(action, null, exceptionCallback);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker Create(Action action, Action successCallback, Action<Exception> exceptionCallback)
        {
            return new Worker(action, successCallback, exceptionCallback);
        }

        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Func<TMessage> factory, Action<TMessage> action)
        {
            return new Worker<TMessage>(factory, action, null, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Func<TMessage> factory, Action<TMessage> action, Action<TMessage> successCallback)
        {
            return new Worker<TMessage>(factory, action, successCallback, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Func<TMessage> factory, Action<TMessage> action, Action<TMessage, Exception> exceptionCallback)
        {
            return new Worker<TMessage>(factory, action, null, exceptionCallback);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Func<TMessage> factory, Action<TMessage> action, Action<TMessage> successCallback, Action<TMessage, Exception> exceptionCallback)
        {
            return new Worker<TMessage>(factory, action, successCallback, exceptionCallback);
        }
    }
}
