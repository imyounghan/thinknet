using System;
using System.Collections.Generic;
using ThinkLib.Interception;

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

        public MessageHandlerAgent(Type messageHandlerInterfaceType, 
            IHandler handler)
            : this(messageHandlerInterfaceType, handler, null)
        { }
        public MessageHandlerAgent(Type messageHandlerInterfaceType,
            IHandler handler,
            IInterceptorProvider interceptorProvider)
            : this(messageHandlerInterfaceType, handler, interceptorProvider, null, null)
        { }

        public MessageHandlerAgent(Type messageHandlerInterfaceType, 
            IHandler handler,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
            : this(messageHandlerInterfaceType, handler, null, firstInterceptors, lastInterceptors)
        { }
        public MessageHandlerAgent(Type messageHandlerInterfaceType, 
            IHandler handler,
            IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
            : base(interceptorProvider, firstInterceptors, lastInterceptors)
        {
            this._targetHandler = handler;
            this._contractType = messageHandlerInterfaceType;
        }

        protected override void TryHandle(object[] args)
        {
            ((dynamic)_targetHandler).Handle((dynamic)args[0]);
        }
        

        protected override Type GetHandlerInterfaceType()
        {
            return this._contractType;
        }

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
