using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;


namespace ThinkNet.Messaging.Handling
{
    public class MessageExecutor : IMessageExecutor, IHandlerProvider, IInterceptionProvider, IInitializer
    {
        private readonly IHandlerRecordStore _handlerStore;
        //private readonly ITextSerializer _serializer;
        private readonly ILogger _logger;
        private readonly ICommandContextFactory _commandContextFactory;
        private readonly IEventContextFactory _eventContextFactory;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageExecutor(IHandlerRecordStore handlerStore, 
            ICommandContextFactory commandContextFactory, 
            IEventContextFactory eventContextFactory)
        {
            this._handlerStore = handlerStore;
            //this._serializer = serializer;
            this._logger = LogManager.GetLogger("ThinkZoo");

            this._commandContextFactory = commandContextFactory;
            this._eventContextFactory = eventContextFactory;
        }


        /// <summary>
        /// 处理当前消息。
        /// </summary>
        protected void ProcessHandler(Type messageType, IMessage message, IProxyHandler messageHandler)
        {
            var @event = message as EventStream;
            if (@event != null) {
                messageHandler.Handle(message);
                return;
            }

            //if (_logger.IsDebugEnabled)
            //    _logger.DebugFormat("Executing a message. messageType:{0}, messageId:{1}", messageType.FullName, message.Id);

            var messageHandlerType = messageHandler.GetInnerHandler().GetType();
            try {
                if (_handlerStore.IsHandlerInfoExist(message.Id, messageType, messageHandlerType)) {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                             messageHandlerType.FullName, messageType.FullName, message.Id);
                    return;
                }

                messageHandler.Handle(message);

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                        messageHandlerType.FullName, messageType.FullName, message.Id);
            }
            catch (Exception ex) {
                if (_logger.IsErrorEnabled) {
                    string errorMessage = string.Format("Exception raised when {0} handling {1}. message info:{2}.",
                        messageHandlerType.FullName, messageType.FullName, message);
                    _logger.Error(errorMessage, ex);
                }

                throw ex;
            }
            finally {
                _handlerStore.AddHandlerInfo(message.Id, messageType, messageHandlerType);
                using (messageHandler as IDisposable) {
                }

                //if (_logger.IsDebugEnabled)
                //    _logger.DebugFormat("Message executed. messageType:{0}, messageId:{1}", messageType.FullName, message.Id);
            }
        }

        /// <summary>
        /// 执行消息
        /// </summary>
        public virtual void Execute(IMessage message)
        {
            var messageType = message.GetType();
            var messageHandlers = this.GetHandlers(messageType);

            if (messageType.IsDefined<RequireHandlerAttribute>(true)) {
                Exception exception = null;

                switch (messageHandlers.Count()) {
                    case 0:
                        exception = new MessageHandlerNotFoundException(messageType);
                        break;
                    case 1:
                        break;
                    default:
                        if (!messageType.GetAttribute<RequireHandlerAttribute>(true).AllowMultiple)
                            exception = new MessageHandlerTooManyException(messageType);
                        break;
                }

                if (exception != null) {
                    _logger.Error(exception.Message);
                    throw exception;
                }
            }

            if (messageHandlers.IsEmpty())
                return;

            List<Exception> innerExceptions = new List<Exception>();
            foreach (var handler in messageHandlers) {
                try {
                    ProcessHandler(messageType, message, handler);
                }
                catch (Exception ex) {
                    innerExceptions.Add(ex);
                }                
            }

            switch (innerExceptions.Count) {
                case 0:
                    break;
                case 1:
                    throw innerExceptions[0];
                default:
                    throw new AggregateException(innerExceptions);
            }
        }


        #region IHandlerProvider 成员

        public IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            List<IProxyHandler> handlerlist = new List<IProxyHandler>();

            var handlerType = typeof(IMessageHandler<>).MakeGenericType(type);
            var handlerWrapperType = typeof(MessageHandlerWrapper<>).MakeGenericType(type);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                .Select(handler => Activator.CreateInstance(handlerWrapperType, new[] { handler }))
                .Cast<IProxyHandler>()
                .AsEnumerable();
            handlerlist.AddRange(handlers);

            if (TypeHelper.IsCommand(type)) {
                handlerType = typeof(ICommandHandler<>).MakeGenericType(type);
                handlerWrapperType = typeof(CommandHandlerWrapper<>).MakeGenericType(type);
                handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                    .Select(handler => Activator.CreateInstance(handlerWrapperType, new[] { handler, _commandContextFactory }))
                    .Cast<IProxyHandler>()
                    .AsEnumerable();
                handlerlist.AddRange(handlers);
            }

            if (TypeHelper.IsEvent(type)) {
                handlerType = typeof(IEventHandler<>).MakeGenericType(type);
                handlerWrapperType = typeof(EventHandlerWrapper<>).MakeGenericType(type);
                handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                    .Select(handler => Activator.CreateInstance(handlerWrapperType, new[] { handler, _eventContextFactory }))
                    .Cast<IProxyHandler>()
                    .AsEnumerable();
                handlerlist.AddRange(handlers);
            }

            return handlerlist;
        }

        #endregion

        #region IInterceptionProvider 成员

        public IEnumerable<IProxyInterception> GetInterceptors(Type type)
        {
            var interceptionType = typeof(IMessageInterception<>).MakeGenericType(type);
            var interceptionWrapperType = typeof(HandlerInterceptionWrapper<>).MakeGenericType(type);
            
            return ServiceLocator.Current.GetAllInstances(interceptionType)
                .Select(filter => Activator.CreateInstance(interceptionWrapperType, new[] { filter }))
                .Cast<IProxyInterception>()
                .AsEnumerable();
        }

        #endregion

        #region IMessageExecutor 成员

        void IMessageExecutor.Execute(IMessage message)
        {
            int count = 0;
            int retryTimes = message is EventStream ? 1 : ConfigurationSetting.Current.HandleRetrytimes;
            while (count++ < retryTimes) {
                try {
                    this.Execute(message);
                    break;
                }
                catch (ThinkNetException) {
                    throw;
                }
                catch (Exception) {
                    if (count == retryTimes)
                        throw;
                    else
                        Thread.Sleep(ConfigurationSetting.Current.HandleRetryInterval);
                }
            }
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            AggregateRootInnerHandlerProvider.Initialize(types);
            types.Where(TypeHelper.IsHandlerType).ForEach(RegisterHandler);
            types.Where(TypeHelper.IsInterceptionType).ForEach(RegisterInterceptor);
        }

        private static void RegisterHandler(Type type)
        {
            var interfaceTypes = type.GetInterfaces().Where(p => TypeHelper.IsCommandHandlerInterfaceType(p) ||
                TypeHelper.IsEventHandlerInterfaceType(p) || TypeHelper.IsMessageHandlerInterfaceType(p));

            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

            foreach (var interfaceType in interfaceTypes) {
                Bootstrapper.Current.RegisterType(interfaceType, type, lifecycle, type.FullName);
            }
        }

        private static void RegisterInterceptor(Type type)
        {
            var interfaceTypes = type.GetInterfaces().Where(TypeHelper.IsMessageInterceptionInterfaceType);

            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

            foreach (var interfaceType in interfaceTypes) {
                Bootstrapper.Current.RegisterType(interfaceType, type, lifecycle, type.FullName);
            }
        }
        #endregion

        
    }
}
