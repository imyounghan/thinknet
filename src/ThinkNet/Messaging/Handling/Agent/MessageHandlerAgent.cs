using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 处理消息的代理程序
    /// </summary>
    public class MessageHandlerAgent : HandlerAgent
    {
        //private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
        //private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

        private readonly IHandler _targetHandler;
        private readonly Type _contractType;
        private readonly IMessageHandlerRecordStore _handlerStore;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageHandlerAgent(Type messageHandlerInterfaceType, 
            IHandler handler, 
            IMessageHandlerRecordStore handlerStore)
        { 
            this._targetHandler = handler;
            this._contractType = messageHandlerInterfaceType;
            this._handlerStore = handlerStore;
        }

        /// <summary>
        /// 尝试处理消息
        /// </summary>
        protected override void TryHandle(object[] args)
        {
            ((dynamic)_targetHandler).Handle((dynamic)args[0]);
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public override void Handle(object[] args)
        {
            var uniquelyId = args[0] as IUniquelyIdentifiable;
            var messageType = args[0].GetType();
            var messageHandlerType = _targetHandler.GetType();

            if(_handlerStore.HandlerIsExecuted(uniquelyId.Id, messageType, messageHandlerType)) {
                var errorMessage = string.Format("The message has been handled. MessageHandlerType:{0}, MessageType:{1}, MessageId:{2}.",
                    messageHandlerType.FullName, messageType.FullName, uniquelyId.Id);
                if(LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.Warn(errorMessage);
                }
                return;
            }

            base.Handle(args);

            _handlerStore.AddHandlerInfo(uniquelyId.Id, messageType, messageHandlerType);
        }

        /// <summary>
        /// 获取消息处理程序
        /// </summary>
        public override object GetInnerHandler()
        {
            return this._targetHandler;
        }
    }

    //public class MessageHandlerAgent<TMessage> : HandlerAgent
    //    where TMessage : class, IMessage
    //{
    //    private readonly IMessageHandler<TMessage> _targetHandler;
    //    private readonly Type _contractType;

    //    public MessageHandlerAgent(IMessageHandler<TMessage> handler)
    //        : this(handler, null)
    //    { }
    //    public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //        IInterceptorProvider interceptorProvider)
    //        : this(handler, interceptorProvider, null, null)
    //    { }

    //    public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //        IEnumerable<IInterceptor> firstInterceptors,
    //        IEnumerable<IInterceptor> lastInterceptors)
    //        : this(handler, null, firstInterceptors, lastInterceptors)
    //    { }
    //    public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //        IInterceptorProvider interceptorProvider,
    //        IEnumerable<IInterceptor> firstInterceptors,
    //        IEnumerable<IInterceptor> lastInterceptors)
    //        : base(interceptorProvider, firstInterceptors, lastInterceptors)
    //    {
    //        this._targetHandler = handler;
    //        this._contractType = typeof(IMessageHandler<TMessage>);
    //    }

    //    //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //    //    FilterHandledMessageInterceptor filterInInterceptor)
    //    //    : this(handler,
    //    //    new IInterceptor[] { filterInInterceptor },
    //    //    new IInterceptor[0])
    //    //{ }

    //    //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //    //    FilterHandledMessageInterceptor filterInInterceptor,
    //    //    NotifyCommandResultInterceptor notifyInterceptor)
    //    //    : this(handler,
    //    //    new IInterceptor[] { filterInInterceptor },
    //    //    new IInterceptor[] { notifyInterceptor })
    //    //{ }

    //    //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
    //    //    IInterceptorProvider interceptorProvider,
    //    //    FilterHandledMessageInterceptor filterInInterceptor,
    //    //    NotifyCommandResultInterceptor notifyInterceptor)
    //    //    : this(handler,
    //    //    interceptorProvider, 
    //    //    new IInterceptor[] { filterInInterceptor }, 
    //    //    new IInterceptor[] { notifyInterceptor })
    //    //{ }

    //    protected override void TryHandle(object[] args)
    //    {            
    //        var message = args[0] as TMessage;
    //        message.NotNull("message");

    //        _targetHandler.Handle(message);
    //    }

    //    protected override Type GetHandlerInterfaceType()
    //    {
    //        return this._contractType;
    //    }

    //    public override object GetInnerHandler()
    //    {
    //        return this._targetHandler;
    //    }
    //}
}
