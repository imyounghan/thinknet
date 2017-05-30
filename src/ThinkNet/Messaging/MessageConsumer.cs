

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
        }

        public MessageConsumer(IMessageReceiver<Envelope<TMessage>> receiver, ILoggerFactory loggerFactory)
            : this(receiver, loggerFactory.GetDefault())
        {
            this._handlers = new Dictionary<Type, ICollection<IHandler>>();
            this._envelopeHandlers = new Dictionary<Type, ICollection<IHandler>>();

            this.messageTypeName = typeof(TMessage).Name.Substring(1);
        }

        /// <summary>
        /// 当收到消息后的处理方法
        /// </summary>
        /// <param name="sender">发送程序</param>
        /// <param name="envelope">一个消息</param>
        protected virtual void OnMessageReceived(object sender, Envelope<TMessage> envelope)
        {
            var messageType = envelope.Body.GetType();

            List<IHandler> combinedHandlers = new List<IHandler>();
            if (this._handlers.ContainsKey(messageType))
            {
                combinedHandlers.AddRange(this._handlers[messageType]);
            }
            if (this._envelopeHandlers.ContainsKey(messageType))
            {
                combinedHandlers.AddRange(this._envelopeHandlers[messageType]);
            }

            foreach(var handler in combinedHandlers)
            {
                if(handler is IEnvelopedHandler) {
                    TryMultipleInvokeHandlerMethod(handler, envelope);
                }
                else {
                    TryMultipleInvokeHandlerMethod(handler, envelope.Body);
                }
            }
        }

        void TryMultipleInvokeHandlerMethod(object handler, object message, int retryTimes = 5, int retryInterval = 1000)
        {
            int count = 0;
            while(count++ < retryTimes) {
                try {
                    ((dynamic)handler).Handle((dynamic)message);
                    break;
                }
                catch(Exception ex) {
                    if(count == retryTimes) {
                        if(logger.IsErrorEnabled) {
                            logger.Error(ex, "Exception raised when handling '{0}' on '{1}'.", message, handler.GetType().FullName);
                        }
                        return;
                    }
                    if(logger.IsWarnEnabled) {
                        logger.Warn(ex,
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             message, handler.GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if(logger.IsDebugEnabled) {
                logger.DebugFormat("Handle '{0}' on '{1}' successfully.",
                    message, handler.GetType().FullName);
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
            }

            var handlers =
                container.ResolveAll(typeof(IMessageHandler<>).MakeGenericType(messageType)).OfType<IHandler>().ToList();

            if (handlers.Count > 0)
            {
                _handlers[messageType] = handlers;
            }
        }

        public virtual void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var messageTypes =
                assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                    .Where(type => type.IsAssignableFrom(typeof(TMessage)))
                    .ToArray();

            foreach (var messageType in messageTypes)
            {
                this.Initialize(container, messageType);
            }
        }

        #endregion
    }
}
