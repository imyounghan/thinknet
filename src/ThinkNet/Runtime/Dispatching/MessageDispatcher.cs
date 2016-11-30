using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib;
using ThinkLib.Composition;
using ThinkNet.Contracts;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理消息的代码程序
    /// </summary>
    public class MessageDispatcher : Dispatcher
    {
        //private readonly FilterHandledMessageInterceptor _first;
        //private readonly NotifyCommandResultInterceptor _last;
        private readonly IMessageHandlerRecordStore _handlerStore;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageDispatcher(IObjectContainer container,
            IMessageHandlerRecordStore handlerStore,
            IMessageBus messageBus,
            ICommandResultNotification notification, 
            IPublishedVersionStore publishedVersionStore)
            : base(container)
        {
            this._handlerStore = handlerStore;
            this.AddCachedHandler(typeof(EventStream).FullName, new EventStreamInnerHandler(container, messageBus, handlerStore, publishedVersionStore));
            this.AddCachedHandler(typeof(CommandResult).FullName, new CommandResultInnerHandler(notification));
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


        //protected virtual IHandlerAgent BuildHandlerAgent(object handler, Type handlerInterfaceType)
        //{
        //    var handlerAgentType = typeof(MessageHandlerAgent<>)
        //        .MakeGenericType(handlerInterfaceType.GetGenericArguments());

        //    return (IHandlerAgent)Activator.CreateInstance(handlerAgentType, handler);
        //    //var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
        //    //return new MessageHandlerAgent(handler, method, null);
        //}

        /// <summary>
        /// 构造消息的处理程序
        /// </summary>
        protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type messageType)
        {
            var contractType = typeof(IMessageHandler<>).MakeGenericType(messageType);

            var handlers = this.GetMessageHandlers(contractType);
            if(handlers.IsEmpty())
                return Enumerable.Empty<IHandlerAgent>();

            //var handlerAgentType = typeof(MessageHandlerAgent<>).MakeGenericType(messageType);
            //var constructor = handlerAgentType.GetConstructor(new Type[] { contractType });

            //return handlers.Select(handler => constructor.Invoke(new object[] { handler })).Cast<IHandlerAgent>();
            return handlers.Select(handler => new MessageHandlerAgent(contractType, handler,_handlerStore)).Cast<IHandlerAgent>();
        }
        
    }
}
