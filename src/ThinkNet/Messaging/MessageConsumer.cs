

namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;

    /// <summary>
    /// 表示 <typeparamref name="TMessage"/> 的消费程序
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    public class MessageConsumer<TMessage> : Processor, IInitializer
    {
        protected enum CheckHandlerMode
        {
            OnlyOne,
            Ignored,
        }


        private readonly Dictionary<Type, ICollection<IHandler>> _handlers;

        private readonly Dictionary<Type, ICollection<IHandler>> _envelopeHandlers;

        protected readonly ILogger logger;



        /// <summary>
        /// 接收消息
        /// </summary>
        private readonly IMessageReceiver<Envelope<TMessage>> receiver;

        /// <summary>
        /// 消息者名称
        /// </summary>
        private readonly string messageTypeName;

        protected MessageConsumer(IMessageReceiver<Envelope<TMessage>> receiver, ILogger logger, string messageTypeName)
            : this(receiver, logger)
        {
            this.messageTypeName = messageTypeName;
        }

        private MessageConsumer(IMessageReceiver<Envelope<TMessage>> receiver, ILogger logger)
        {
            this.receiver = receiver;
            this.logger = logger;
            this.CheckMode = CheckHandlerMode.Ignored;
        }

        public MessageConsumer(IMessageReceiver<Envelope<TMessage>> receiver, ILoggerFactory loggerFactory)
            : this(receiver, loggerFactory.GetDefault())
        {
            this._handlers = new Dictionary<Type, ICollection<IHandler>>();
            this._envelopeHandlers = new Dictionary<Type, ICollection<IHandler>>();

            this.messageTypeName = typeof(TMessage).Name.Substring(1);
        }

        protected CheckHandlerMode CheckMode { get; set; }

        protected IEnumerable<IHandler> GetHandlers(Type messageType)
        {
            List<IHandler> combinedHandlers = new List<IHandler>();
            if(this._handlers.ContainsKey(messageType)) {
                combinedHandlers.AddRange(this._handlers[messageType]);
            }
            if(this._envelopeHandlers.ContainsKey(messageType)) {
                combinedHandlers.AddRange(this._envelopeHandlers[messageType]);
            }

            return combinedHandlers;
        }

        /// <summary>
        /// 当收到消息后的处理方法
        /// </summary>
        /// <param name="sender">发送程序</param>
        /// <param name="envelope">一个消息</param>
        protected virtual void OnMessageReceived(object sender, Envelope<TMessage> envelope)
        {
            try
            {
                this.ProcessMessage(envelope, envelope.Body.GetType());
            }
            catch (PublishableException)
            {
            }
            catch (Exception)
            {
                if(logger.IsErrorEnabled) {
                    //logger.Error(ex, "Exception raised when handling '{0}' on '{1}'.",
                    //    getMessageData(message), getHandlerName(handler));
                }
            }

        }

        protected void ProcessMessage(Envelope<TMessage> envelope, Type messageType)
        {
            var combinedHandlers = this.GetHandlers(messageType);

            if(combinedHandlers.IsEmpty()) {
                if(logger.IsWarnEnabled) {
                    logger.WarnFormat("There is no handler of type('{0}').", messageType.FullName);
                }
                return;
            }
            
            foreach(var handler in combinedHandlers) {
                if(handler is IEnvelopedHandler) {
                    TryMultipleInvoke(InvokeHandler, handler, envelope);
                }
                else {
                    TryMultipleInvoke(InvokeHandler, handler, envelope.Body);
                }
            }
        }

        private void InvokeHandler<THandler, TParameter>(THandler handler, TParameter parameter)
        {
            ((dynamic)handler).Handle((dynamic)parameter);
        }

        protected void TryMultipleInvoke<THandler, TParameter>(
            Action<THandler, TParameter> action,
            THandler handler,
            TParameter parameter,
            int retryTimes = 5,
            int retryInterval = 1000)
        {
            this.TryMultipleInvoke(action, handler, parameter, 
                handler1 => handler1.GetType().FullName, message1 => message1.ToString(),
                retryTimes, retryInterval);
        }

        protected void TryMultipleInvoke<THandler, TParameter>(Action<THandler, TParameter> action,
            THandler handler, TParameter parameter,
            Func<THandler, string> getHandlerName, Func<TParameter, string> getParameterData,  
            int retryTimes = 5, int retryInterval = 1000)
        {
            int count = 0;
            while(count++ < retryTimes) {
                try
                {
                    action(handler, parameter);
                    break;
                }
                catch(PublishableException ex) {
                    if(logger.IsErrorEnabled) {
                        logger.Error(ex, "PublishableException raised when handling '{0}' on '{1}', Error will be send to bus.",
                            getParameterData(parameter), getHandlerName(handler));
                    }
                    throw ex;
                }
                catch(Exception ex) {
                    if(count == retryTimes) {
                        if(logger.IsErrorEnabled) {
                            logger.Error(ex, "Exception raised when handling '{0}' on '{1}'.",
                                getParameterData(parameter), getHandlerName(handler));
                        }
                        throw ex;
                    }
                    if(logger.IsWarnEnabled) {
                        logger.Warn(ex,
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             getParameterData(parameter), getHandlerName(handler), count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if(logger.IsDebugEnabled) {
                logger.DebugFormat("Handle '{0}' on '{1}' successfully.",
                    getParameterData(parameter), getHandlerName(handler));
            }

        }


        /// <summary>
        /// 启动进程
        /// </summary>
        protected override void Start()
        {
            this.receiver.MessageReceived += this.OnMessageReceived;
            this.receiver.Start();

            Console.WriteLine("{0} Consumer Started!", messageTypeName);
        }

        /// <summary>
        /// 停止进程
        /// </summary>
        protected override void Stop()
        {
            this.receiver.MessageReceived -= this.OnMessageReceived;
            this.receiver.Stop();

            Console.WriteLine("{0} Consumer Stopped!", messageTypeName);
        }

        #region IInitializer 成员

        protected virtual void Initialize(IObjectContainer container, Type messageType)
        {
            var envelopedEventHandlers =
                container.ResolveAll(typeof(IEnvelopedMessageHandler<>).MakeGenericType(messageType))
                    .OfType<IEnvelopedHandler>()
                    .Cast<IHandler>()
                    .ToList();
            if (envelopedEventHandlers.Count > 0)
            {
                _envelopeHandlers[messageType] = envelopedEventHandlers;

                if(this.CheckMode == CheckHandlerMode.OnlyOne)
                {
                    if (envelopedEventHandlers.Count > 1)
                    {
                        throw new SystemException(
                            string.Format(
                                "Found more than one handler for '{0}' with IEnvelopedMessageHandler<>.",
                                messageType.FullName));
                    }

                    return;
                }
            }

            var handlers =
                container.ResolveAll(typeof(IMessageHandler<>).MakeGenericType(messageType)).OfType<IHandler>().ToList();
            if(this.CheckMode == CheckHandlerMode.OnlyOne)
            {
                switch(handlers.Count) {
                    case 0:
                        throw new SystemException(string.Format("The type('{0}') of handler is not found.", messageType.FullName));
                    case 1:
                        break;
                    default:
                        throw new SystemException(string.Format("Found more than one handler for '{0}' with IMessageHandler<>.", messageType.FullName));
                }
            }

            if (handlers.Count > 0)
            {
                _handlers[messageType] = handlers;
            }
        }

        public virtual void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var baseType = typeof(TMessage);

            var messageTypes =
                assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                    .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                    .ToArray();

            foreach (var messageType in messageTypes)
            {
                this.Initialize(container, messageType);
            }
        }

        #endregion
    }
}
