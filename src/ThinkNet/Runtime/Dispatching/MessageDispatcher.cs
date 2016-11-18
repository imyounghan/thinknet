using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理消息的代码程序
    /// </summary>
    public class MessageDispatcher : IDispatcher
    {
        private readonly ConcurrentDictionary<string, IHandlerAgent> _cachedHandlers;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageDispatcher(IHandlerRecordStore handlerStore,
            IMessageBus messageBus,
            ICommandResultNotification notification)
            : this()
        {
            this.AddCachedHandler(typeof(EventStream).FullName, 
                new EventStreamInnerHandler(
                    new InterceptorPipeline(new IInterceptor[] { 
                        new FilterHandledMessageInterceptor(handlerStore), 
                        new NotifyCommandResultInterceptor(messageBus) 
                    })
                )
            );
            this.AddCachedHandler(typeof(CommandResultReplied).FullName, new CommandResultRepliedInnerHandler(notification));
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        protected MessageDispatcher()
        {
            this._cachedHandlers = new ConcurrentDictionary<string, IHandlerAgent>();
        }
        /// <summary>
        /// 添加一个缓存的Handler
        /// </summary>
        protected void AddCachedHandler(string key, IHandlerAgent handler)
        {
            _cachedHandlers.TryAdd(key, handler);
        }
        ///// <summary>
        ///// 获取一个Hanlder,如果不存在则添加一个Handler
        ///// </summary>
        //protected IHandlerProxy GetOrAddHandler(string key, Func<IHandlerProxy> handlerFactory)
        //{
        //    return _cachedHandlers.GetOrAdd(key, handlerFactory);
        //}
        ///// <summary>
        ///// 从缓存中获取一个Handler
        ///// </summary>
        //protected IHandlerProxy GetCachedHandler(string key)
        //{
        //    return _cachedHandlers.GetOrDefault(key, (IHandlerProxy)null);
        //}


        private IHandlerAgent BuildMessageHandler(object handler, Type contractType)
        {
            var method = HandlerMethodProvider.Instance.GetCachedMethodInfo(contractType, () => handler.GetType());
            return new MessageHandlerAgent(handler, method, null);
        }

        /// <summary>
        /// 构造消息的处理程序
        /// </summary>
        protected virtual IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type)
        {
            Type contractType;
            var handlers = HandlerFetchedProvider.Instance.GetMessageHandlers(type, out contractType);
            return handlers.Select(handler => BuildMessageHandler(handler, contractType)); ;
        }

        /// <summary>
        /// 获取消息的处理程序
        /// </summary>
        private IEnumerable<IHandlerAgent> GetProxyHandlers(Type type)
        {
            var key = type.FullName;
            IHandlerAgent cachedHandler;
            if (_cachedHandlers.TryGetValue(key, out cachedHandler))
                yield return cachedHandler;

            var handlers = BuildHandlerAgents(type);
            foreach (var handler in handlers) {
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
            foreach (var handler in handlers) {
                try {
                    stopwatch.Restart();
                    handler.Handle(message);
                    stopwatch.Stop();

                    executionTime += stopwatch.Elapsed;
                }
                catch (Exception ex) {
                    if (LogManager.Default.IsErrorEnabled) {
                        LogManager.Default.Error(ex, "Exception raised when handling {0}.", message);
                    }
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
