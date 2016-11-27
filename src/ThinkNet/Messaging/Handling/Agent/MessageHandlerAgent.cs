using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThinkLib;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;
using ThinkNet.Runtime;

namespace ThinkNet.Messaging.Handling.Agent
{
    ///// <summary>
    ///// 处理消息的代理程序
    ///// </summary>
    //public class MessageHandlerAgent : HandlerAgent
    //{
    //    private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
    //    private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

    //    /// <summary>
    //    /// Parameterized Constructor.
    //    /// </summary>
    //    protected MessageHandlerAgent(InterceptorPipeline pipeline)
    //        : base(pipeline)
    //    { }
    //    /// <summary>
    //    /// Parameterized Constructor.
    //    /// </summary>
    //    public MessageHandlerAgent(object handler, MethodInfo method, InterceptorPipeline pipeline)
    //        : base(handler, method, pipeline)
    //    { }

    //    /// <summary>
    //    /// 尝试多次处理，默认只处理一次
    //    /// </summary>
    //    protected virtual void TryHandle(object[] args)
    //    {
    //        ReflectedMethod.Invoke(HandlerInstance, args);
    //    }

    //    protected override void TryMultipleHandle(object[] args)
    //    {
    //        int count = 0;
    //        while(count++ < retryTimes) {
    //            try {
    //                this.TryHandle(args);
    //                break;
    //            }
    //            catch(ThinkNetException) {
    //                throw;
    //            }
    //            catch(Exception ex) {
    //                if(count == retryTimes) {
    //                    throw new ThinkNetException(ex.Message, ex);
    //                }
    //                if(LogManager.Default.IsWarnEnabled) {
    //                    LogManager.Default.Warn(ex,
    //                        "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
    //                         args.Last(), HandlerInstance.GetType().FullName, count);
    //                }
    //                Thread.Sleep(retryInterval);
    //            }
    //        }

    //        if(LogManager.Default.IsDebugEnabled) {
    //            LogManager.Default.DebugFormat("Handle '{0}' on '{1}' success.", args.Last(), HandlerInstance.GetType().FullName);
    //        }
    //    }
    //}

    public class MessageHandlerAgent<TMessage> : HandlerAgent
        where TMessage : class, IMessage
    {
        private readonly IMessageHandler<TMessage> _targetHandler;
        private readonly Type _contractType;

        public MessageHandlerAgent(IMessageHandler<TMessage> handler)
            : this(handler, null)
        { }
        public MessageHandlerAgent(IMessageHandler<TMessage> handler,
            IInterceptorProvider interceptorProvider)
            : this(handler, interceptorProvider, null, null)
        { }

        public MessageHandlerAgent(IMessageHandler<TMessage> handler,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
            : this(handler, null, firstInterceptors, lastInterceptors)
        { }
        public MessageHandlerAgent(IMessageHandler<TMessage> handler,
            IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
            : base(interceptorProvider, firstInterceptors, lastInterceptors)
        {
            this._targetHandler = handler;
            this._contractType = typeof(IMessageHandler<TMessage>);
        }

        //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
        //    FilterHandledMessageInterceptor filterInInterceptor)
        //    : this(handler,
        //    new IInterceptor[] { filterInInterceptor },
        //    new IInterceptor[0])
        //{ }

        //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
        //    FilterHandledMessageInterceptor filterInInterceptor,
        //    NotifyCommandResultInterceptor notifyInterceptor)
        //    : this(handler,
        //    new IInterceptor[] { filterInInterceptor },
        //    new IInterceptor[] { notifyInterceptor })
        //{ }

        //public MessageHandlerAgent(IMessageHandler<TMessage> handler,
        //    IInterceptorProvider interceptorProvider,
        //    FilterHandledMessageInterceptor filterInInterceptor,
        //    NotifyCommandResultInterceptor notifyInterceptor)
        //    : this(handler,
        //    interceptorProvider, 
        //    new IInterceptor[] { filterInInterceptor }, 
        //    new IInterceptor[] { notifyInterceptor })
        //{ }

        protected override void TryHandle(object[] args)
        {            
            var message = args[0] as TMessage;
            message.NotNull("message");

            _targetHandler.Handle(message);
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
}
