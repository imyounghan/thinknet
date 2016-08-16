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
        public static Worker<TMessage> Create<TMessage>(Action<TMessage> action, Func<TMessage> factory)
        {
            return new Worker<TMessage>(action, factory, null, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Action<TMessage> action, Func<TMessage> factory, Action<TMessage> successCallback)
        {
            return new Worker<TMessage>(action, factory, successCallback, null);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Action<TMessage> action, Func<TMessage> factory, Action<TMessage, Exception> exceptionCallback)
        {
            return new Worker<TMessage>(action, factory, null, exceptionCallback);
        }
        /// <summary>
        /// 创建一个后台线程工作器
        /// </summary>
        public static Worker<TMessage> Create<TMessage>(Action<TMessage> action, Func<TMessage> factory, Action<TMessage> successCallback, Action<TMessage, Exception> exceptionCallback)
        {
            return new Worker<TMessage>(action, factory, successCallback, exceptionCallback);
        }
    }
}
