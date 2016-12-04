using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ThinkNet.Infrastructure.Composition;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// <see cref="IDispatcher"/> 的抽象实现类
    /// </summary>
    public abstract class Dispatcher : IDispatcher, IInitializer
    {
        private readonly ConcurrentDictionary<string, IHandlerAgent> _cachedHandlers;
        //private readonly static ConcurrentDictionary<string, Lifecycle> 

        private readonly IObjectContainer _container;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected Dispatcher(IObjectContainer container)
        {
            this._container = container;
            this._cachedHandlers = new ConcurrentDictionary<string, IHandlerAgent>();
        }
        /// <summary>
        /// 添加一个缓存的Handler
        /// </summary>
        protected void AddCachedHandler(string key, IHandlerAgent handler)
        {
            _cachedHandlers.TryAdd(key, handler);
        }

        /// <summary>
        /// 获取消息处理器
        /// </summary>
        protected IEnumerable<IHandler> GetMessageHandlers(Type contractType)
        {
            return _container.ResolveAll(contractType).Cast<IHandler>();
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
            if(_cachedHandlers.TryGetValue(type.FullName, out cachedHandler))
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

        #region IInitializer 成员

        void IInitializer.Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            _cachedHandlers.Values.OfType<IInitializer>()
                .ForEach(item => item.Initialize(container, assemblies));
        }

        #endregion
    }
}
