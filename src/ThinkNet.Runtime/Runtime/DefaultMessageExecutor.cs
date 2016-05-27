using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Runtime
{
    public class DefaultMessageExecutor : IMessageExecutor
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly ILogger _logger;
        private readonly ICommandContextFactory _commandContextFactory;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public DefaultMessageExecutor(IHandlerRecordStore handlerStore, ICommandContextFactory commandContextFactory)
        {
            this._handlerStore = handlerStore;
            this._commandContextFactory = commandContextFactory;
            this._logger = LogManager.GetLogger("ThinkNet");
        }


        /// <summary>
        /// 处理当前消息。
        /// </summary>
        private void ProcessHandler(HandlerWrapper handler)
        {
            try {
                if (_handlerStore.HandlerIsExecuted(handler.MessageId, handler.MessageType, handler.HanderType)) {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                             handler.HanderType.FullName, handler.MessageType.FullName, handler.MessageId);
                    return;
                }
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Check the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    handler.HanderType.FullName, handler.MessageType.FullName, handler.MessageId);
                _logger.Warn(errorMsg, ex);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }

            try {
                using (handler as IDisposable) { }

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                        handler.HanderType.FullName, handler.MessageType.FullName, handler.MessageId);
            }
            catch (Exception ex) {
                if (_logger.IsErrorEnabled) {
                    string errorMessage = string.Format("Exception raised when {0} handling {1}. message info:{2}.",
                        handler.HanderType.FullName, handler.MessageType.FullName, handler.MessageId);
                    _logger.Error(errorMessage, ex);
                }

                throw ex;
            }

            try {
                _handlerStore.AddHandlerInfo(handler.MessageId, handler.MessageType, handler.HanderType);
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Save the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    handler.HanderType.FullName, handler.MessageType.FullName, handler.MessageId);
                _logger.Warn(errorMsg, ex);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }
        }


        /// <summary>
        /// 执行消息
        /// </summary>
        private void Execute(IMessage message)
        {
            var handlers = this.GetHandlers(message);

            if (handlers.IsEmpty())
                return;

            //var interceptors = GetInterceptors(messageType);

            //var handled = messageHandlers.Any(handler => _handlerStore.HandlerIsExecuted(message.Id, messageType, handler.Key));
            //if (!handled) {
            //    interceptors.ForEach(proxy => proxy.OnHandlerExecuting(message));
            //}

            List<Exception> innerExceptions = new List<Exception>();
            foreach (var handler in handlers) {
                try {
                    ProcessHandler(handler);
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

            //if (!handled) {
            //    interceptors.Reverse().ForEach(proxy => proxy.OnHandlerExecuted(message, exception));
            //}

            if (exception != null)
                throw exception;
        }

        private IEnumerable<HandlerWrapper> GetHandlers(IMessage message)
        {
            List<HandlerWrapper> handlerlist = new List<HandlerWrapper>();

            var messageType = message.GetType();

            var handlerType = typeof(IHandler<>).MakeGenericType(messageType);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                .Cast<IHandler>()
                .Select(handler => new HandlerWrapper(handler, message))
                .ToArray();
            handlerlist.AddRange(handlers);

            if (TypeHelper.IsCommand(messageType)) {
                handlerType = typeof(ICommandHandler<>).MakeGenericType(messageType);
                handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                    .Cast<IHandler>()
                    .Select(handler => new CommandHandlerWrapper(handler, message, _commandContextFactory))
                    .ToArray();

                switch (handlers.Length) {
                    case 0:
                        if (handlerlist.Count == 0)
                            throw new MessageHandlerNotFoundException(messageType);
                        else if(handlerlist.Count > 1)
                            throw new MessageHandlerTooManyException(messageType);
                        break;
                    case 1:
                        handlerlist.Clear();
                        handlerlist.AddRange(handlers);
                        break;
                    default:
                        throw new MessageHandlerTooManyException(messageType);
                }                
            }

            return handlerlist;
        }


        #region IMessageExecutor 成员

        void IMessageExecutor.Execute(object message)
        {
            int count = 0;
            int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;


            while (count++ < retryTimes) {
                try {
                    this.Execute(message as IMessage);
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
        

        class HandlerWrapper : DisposableObject
        {
            private readonly IMessage message;
            private readonly Type messageType;
            private readonly IHandler handler;
            private readonly Type handerType;
            private readonly Lifecycle lifetime;

            public HandlerWrapper(IHandler handler, IMessage message)
            {
                this.message = message;
                this.messageType = message.GetType();
                this.handler = handler;
                this.handerType = handler.GetType();
                this.lifetime = LifeCycleAttribute.GetLifecycle(handerType);
            }

            //public void Handle()
            //{
            //    this.Execute(handler, message);
            //}

            protected virtual void Execute(IHandler handler, IMessage message)
            {
                ((dynamic)handler).Handle((dynamic)message);
            }

            /// <summary>
            /// dispose
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                this.Execute(handler, message);
                if (lifetime != Lifecycle.Singleton && disposing) {
                    using (handler as IDisposable) {
                        // Dispose handler if it's disposable.
                    }
                }
            }

            public string MessageId { get { return this.message.Id; } }

            public Type MessageType { get { return this.messageType; } }

            public Type HanderType { get { return this.handerType; } }

            public IHandler GetInnerHandler()
            {
                return this.handler;
            }
        }

        class CommandHandlerWrapper : HandlerWrapper
        {
            private readonly ICommandContextFactory commandContextFactory;

            public CommandHandlerWrapper(IHandler handler, IMessage message, ICommandContextFactory commandContextFactory)
                : base(handler, message)
            {
                this.commandContextFactory = commandContextFactory;
            }

            protected override void Execute(IHandler handler, IMessage message)
            {
                var context = commandContextFactory.CreateCommandContext();
                ((dynamic)handler).Handle(context, (dynamic)message);
                context.Commit(message.Id);
            }
        }
    }
}
