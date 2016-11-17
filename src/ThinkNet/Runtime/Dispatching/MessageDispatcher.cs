using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Common.Composition;
using ThinkNet.Contracts;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Proxies;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理消息的代码程序
    /// </summary>
    public class MessageDispatcher : IDispatcher, IInitializer
    {
        private readonly ConcurrentDictionary<string, IHandlerProxy> _cachedHandlers;
        private readonly IHandlerMethodProvider _handlerMethodProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageDispatcher(IHandlerMethodProvider handlerMethodProvider,
            IHandlerRecordStore handlerStore,
            IMessageBus messageBus,
            ICommandResultNotification notification)
            : this()
        {
            this._handlerMethodProvider = handlerMethodProvider;

            this.AddHandler(typeof(EventStream).FullName, new EventStreamInnerHandler(handlerMethodProvider));
            this.AddHandler(typeof(CommandResultReplied).FullName, new CommandResultRepliedInnerHandler(notification));
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected MessageDispatcher()
        {
            this._cachedHandlers = new ConcurrentDictionary<string, IHandlerProxy>();
        }
        /// <summary>
        /// 添加一个Handler
        /// </summary>
        protected void AddHandler(string key, IHandlerProxy handler)
        {
            _cachedHandlers.TryAdd(key, handler);
        }
        /// <summary>
        /// 获取一个Hanlder,如果不存在则添加一个Handler
        /// </summary>
        protected IHandlerProxy GetOrAddHandler(string key, Func<IHandlerProxy> handlerFactory)
        {
            return _cachedHandlers.GetOrAdd(key, handlerFactory);
        }
        /// <summary>
        /// 从缓存中获取一个Handler
        /// </summary>
        protected IHandlerProxy GetCachedHandler(string key)
        {
            return _cachedHandlers.GetOrDefault(key, (IHandlerProxy)null);
        }

       
        private IHandlerProxy BuildMessageHandler(object handler, Type contractType)
        {
            var method = _handlerMethodProvider.GetCachedMethodInfo(handler.GetType(), contractType);
            return new MessageHandlerProxy(handler, method, null);
        }

        /// <summary>
        /// 获取消息的处理程序
        /// </summary>
        protected virtual IEnumerable<IHandlerProxy> GetProxyHandlers(Type type)
        {
            var key = type.FullName;
            IHandlerProxy cachedHandler;
            if (_cachedHandlers.TryGetValue(key, out cachedHandler))
                yield return cachedHandler;

            var contractType = typeof(IMessageHandler<>).MakeGenericType(type);
            var handlers = ObjectContainer.Instance.ResolveAll(contractType)
                .Select(handler => BuildMessageHandler(handler, contractType));
            foreach (var handler in handlers) {
                yield return handler;
            }
        }

        #region IInitializer 成员
        /// <summary>
        /// 初始化程序
        /// </summary>
        public virtual void Initialize(IEnumerable<Type> types)
        {
            _cachedHandlers.Values.OfType<IInitializer>()
                .ForEach(delegate(IInitializer initializer) {
                    initializer.Initialize(types);
                });
        }

        #endregion


        /// <summary>
        /// 执行消息结果
        /// </summary>
        public void Execute(IMessage message, out TimeSpan executionTime)
        {
            message.NotNull("message");

            executionTime = TimeSpan.Zero;


            var stopwatch = Stopwatch.StartNew();
            var handlers = GetProxyHandlers(message.GetType());
            foreach (var handler in handlers) {
                try {
                    stopwatch.Restart();
                    handler.Handle(message);
                    stopwatch.Stop();

                    executionTime += stopwatch.Elapsed;
                }
                catch (Exception ex) {
                    //TODO....WriteLog
                    //this.OnException(ex);
                }
            }
        }

        //protected virtual void OnExecuted(TMessage message, ExecutionStatus status)
        //{
        //    if (LogManager.Default.IsDebugEnabled) {
        //        LogManager.Default.DebugFormat("Handle {0} success.", message);
        //    }
        //}

        //protected virtual void OnException(TMessage message, ThinkNetException ex)
        //{
        //    if (LogManager.Default.IsErrorEnabled) {
        //        LogManager.Default.Error(ex, "Exception raised when handling {0}.", message);
        //    }
        //}        
    }
}
