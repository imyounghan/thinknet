using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using ThinkLib;
using ThinkLib.Composition;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    public abstract class Dispatcher : IDispatcher
    {
        private readonly static ConcurrentDictionary<string, IHandlerAgent> CachedHandlers = new ConcurrentDictionary<string, IHandlerAgent>();
        //private readonly static ConcurrentDictionary<string, Lifecycle> 

        private readonly IObjectContainer _container;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Dispatcher(IObjectContainer container)
        {
            this._container = container;
        }
        /// <summary>
        /// 添加一个缓存的Handler
        /// </summary>
        protected void AddCachedHandler(string key, IHandlerAgent handler)
        {
            CachedHandlers.TryAdd(key, handler);
        }

        /// <summary>
        /// 获取消息处理器
        /// </summary>
        protected IEnumerable<object> GetMessageHandlers(Type contractType)
        {
            return _container.ResolveAll(contractType);
        }


        /// <summary>
        /// 构造消息的处理程序
        /// </summary>
        protected abstract IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type);

        /// <summary>
        /// 获取消息的处理程序
        /// </summary>
        private IEnumerable<IHandlerAgent> GetProxyHandlers(Type type)
        {
            IHandlerAgent cachedHandler;
            if(CachedHandlers.TryGetValue(type.FullName, out cachedHandler))
                yield return cachedHandler;

            var handlers = BuildHandlerAgents(type);
            foreach(var handler in handlers) {
                yield return handler;
            }
        }


        /// <summary>
        /// 执行消息结果
        /// </summary>
        public void Execute(IMessage message, out TimeSpan executionTime)
        {
            message.NotNull("message");

            executionTime = TimeSpan.Zero;


            var stopwatch = Stopwatch.StartNew();
            var handlers = GetProxyHandlers(message.GetType());
            foreach(var handler in handlers) {
                try {
                    stopwatch.Restart();
                    handler.Handle(message);
                    stopwatch.Stop();

                    executionTime += stopwatch.Elapsed;
                }
                catch(Exception ex) {
                    if(LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error(ex, "Exception raised when handling {0}.", message);
                    }
                }
            }
        }
    }
}
