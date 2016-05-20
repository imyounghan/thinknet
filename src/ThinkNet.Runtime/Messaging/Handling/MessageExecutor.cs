using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;


namespace ThinkNet.Messaging.Handling
{
    public class MessageExecutor : IMessageExecutor, IInitializer
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly ILogger _logger;
        private readonly ICommandContextFactory _commandContextFactory;
        //private readonly IEventContextFactory _eventContextFactory;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageExecutor(IHandlerRecordStore handlerStore, 
            ICommandContextFactory commandContextFactory/*, 
            IEventContextFactory eventContextFactory*/)
        {
            this._handlerStore = handlerStore;
            this._logger = LogManager.GetLogger("ThinkZoo");

            this._commandContextFactory = commandContextFactory;
            //this._eventContextFactory = eventContextFactory;
        }


        /// <summary>
        /// 处理当前消息。
        /// </summary>
        protected virtual void ProcessHandler(Type messageType, IMessage message, Type messageHandlerType, IProxyHandler messageHandler)
        {
            if (message is EventStream) {
                messageHandler.Handle(message);
                return;
            }
            
            try {
                if (_handlerStore.HandlerIsExecuted(message.Id, messageType, messageHandlerType)) {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                             messageHandlerType.FullName, messageType.FullName, message.Id);
                    return;
                }
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Check the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    messageHandlerType.FullName, messageType.FullName, message.Id);
                _logger.Warn(errorMsg, ex);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }

            try {
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
                using (messageHandler as IDisposable) { }
            }

            try {
                _handlerStore.AddHandlerInfo(message.Id, messageType, messageHandlerType);
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Save the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    messageHandlerType.FullName, messageType.FullName, message.Id);
                _logger.Warn(errorMsg, ex);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }
        }

        private void ProcessAllHandler(Type messageType, IMessage message, IEnumerable<KeyValuePair<Type, IProxyHandler>> messageHandlers)
        {
            var interceptors = GetInterceptors(messageType);

            var handled = _handlerStore.HandlerHasExecuted(message.Id, messageType, messageHandlers.Select(p => p.Key).ToArray());
            if (!handled) {
                interceptors.ForEach(proxy => proxy.OnHandlerExecuting(message));
            }

            List<Exception> innerExceptions = new List<Exception>();
            foreach (var handler in messageHandlers) {
                try {
                    ProcessHandler(messageType, message, handler.Key, handler.Value);
                }
                catch (Exception ex) {
                    innerExceptions.Add(ex);
                }
            }

            Exception exception = null;
            switch (innerExceptions.Count) {
                case 0:
                    break;
                case 1:
                    exception = innerExceptions[0];
                    break;
                default:
                    exception = new AggregateException(innerExceptions);
                    break;
            }

            if (!handled) {
                interceptors.ForEach(proxy => proxy.OnHandlerExecuted(message, exception));
            }

            if (exception != null)
                throw exception;
        }

        /// <summary>
        /// 执行消息
        /// </summary>
        private void Execute(IMessage message)
        {
            var messageType = message.GetType();

            var messageHandlers = this.GetHandlers(messageType);

            if (message is ICommand) {
                Exception exception = null;
                switch (messageHandlers.Count()) {
                    case 0:
                        exception = new MessageHandlerNotFoundException(messageType);
                        break;
                    case 1:
                        break;
                    default:
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

            var proxyHandlers = messageHandlers.Select(p => new KeyValuePair<Type, IProxyHandler>(p.GetInnerHandler().GetType(), p));
            ProcessAllHandler(messageType, message, proxyHandlers);
            //List<Exception> innerExceptions = new List<Exception>();
            //foreach (var handler in messageHandlers) {
            //    try {
            //        ProcessHandler(messageType, message, handler);
            //    }
            //    catch (Exception ex) {
            //        innerExceptions.Add(ex);
            //    }
            //}

            //switch (innerExceptions.Count) {
            //    case 0:
            //        break;
            //    case 1:
            //        throw innerExceptions[0];
            //    default:
            //        throw new AggregateException(innerExceptions);
            //}
        }

        #region IHandlerProvider 成员

        public IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            List<IProxyHandler> handlerlist = new List<IProxyHandler>();

            //Type handlerType;
            //Type handlerWrapperType;
            //IEnumerable<IProxyHandler> handlers;

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

            //if (TypeHelper.IsEvent(type) && (handlingFlags & HandlingFlags.EventHandler) == HandlingFlags.EventHandler) {
            //    handlerType = typeof(IEventHandler<>).MakeGenericType(type);
            //    handlerWrapperType = typeof(EventHandlerWrapper<>).MakeGenericType(type);
            //    handlers = ServiceLocator.Current.GetAllInstances(handlerType)
            //        .Select(handler => Activator.CreateInstance(handlerWrapperType, new[] { handler, _eventContextFactory }))
            //        .Cast<IProxyHandler>()
            //        .AsEnumerable();
            //    handlerlist.AddRange(handlers);
            //}

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
            var interfaceTypes = type.GetInterfaces().Where(TypeHelper.IsHandlerInterfaceType);

            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

            foreach (var interfaceType in interfaceTypes) {
                Bootstrapper.Current.RegisterType(interfaceType, type, lifecycle, type.FullName);
            }
        }

        private static void RegisterInterceptor(Type type)
        {
            var interfaceTypes = type.GetInterfaces().Where(TypeHelper.IsInterceptionInterfaceType);

            var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);

            foreach (var interfaceType in interfaceTypes) {
                Bootstrapper.Current.RegisterType(interfaceType, type, lifecycle, type.FullName);
            }
        }
        #endregion

        
    }
}
