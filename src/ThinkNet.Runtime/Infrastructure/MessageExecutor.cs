using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib.Logging;
using ThinkNet.Kernel;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Infrastructure
{
    public class MessageExecutor : IMessageExecutor
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly IHandlerProvider _handlerProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageExecutor(IHandlerProvider handlerProvider,
            IHandlerRecordStore handlerStore)
        {
            this._handlerProvider = handlerProvider;
            this._handlerStore = handlerStore;
        }

        /// <summary>
        /// 处理当前消息。
        /// </summary>
        protected void ProcessHandler(Type messageType, IMessage message, IProxyHandler messageHandler)
        {
            var messageHandlerType = messageHandler.GetInnerHandler().GetType();
            var messageHandlerTypeName = messageHandlerType.FullName;
            var messageTypeName = messageType.FullName;
            

            try {
                if (message is DomainEventStream) {
                    messageHandler.Handle(message);
                    return;
                }

                if (_handlerStore.IsHandlerInfoExist(message.Id, messageTypeName, messageHandlerTypeName)) {
                    LogManager.GetLogger("ThinkNet").DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                        messageHandlerTypeName, messageTypeName, message.Id);
                    return;
                }

                messageHandler.Handle(message);
                _handlerStore.AddHandlerInfo(message.Id, messageTypeName, messageHandlerTypeName);

                LogManager.GetLogger("ThinkNet").DebugFormat("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    messageHandlerTypeName, messageTypeName, message.Id);
            }
            catch (Exception ex) {
                if (!(message is DomainEventStream)) {
                    string errorMessage = string.Format("Exception raised when {0} handling {1}. message info:{2}.",
                        messageHandlerTypeName, messageTypeName, message.ToString());
                    LogManager.GetLogger("ThinkNet").Error(errorMessage, ex);
                }

                throw ex;
            }
            finally {
                if (messageHandler is IDisposable) {
                    ((IDisposable)messageHandler).Dispose();
                }
            }
        }

        /// <summary>
        /// 执行消息
        /// </summary>
        public virtual void Execute(IMessage message)
        {
            var messageType = message.GetType();
            var messageHandlers = _handlerProvider.GetHandlers(messageType);

            if (messageHandlers.IsEmpty()) {
                var exception = new MessageHandlerNotFoundException(messageType);
                LogManager.GetLogger("ThinkNet").Warn(exception.Message);

                return;
            }

            if (messageType.IsDefined<HandleOnlyOnceAttribute>(true) && messageHandlers.Count() > 1) {
                var exception = new MessageHandlerTooManyException(messageType);
                LogManager.GetLogger("ThinkNet").Error(exception.Message);

                throw exception;
            }

            foreach (var handler in messageHandlers) {
                ProcessHandler(messageType, message, handler);
            }
        }
    }
}
