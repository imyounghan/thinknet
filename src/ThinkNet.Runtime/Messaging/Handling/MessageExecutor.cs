﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Logging;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;


namespace ThinkNet.Messaging.Handling
{
    public class MessageExecutor : IMessageExecutor, IHandlerProvider
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly ILogger _logger;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageExecutor(IHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
            this._logger = LogManager.GetLogger("ThinkNet");
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

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("executing a message. data:{0}",
                    DefaultTextSerializer.Instance.Serialize(message));

            var messageHandlerType = messageHandler.GetInnerHandler().GetType();
            try {
                if (_handlerStore.IsHandlerInfoExist(message.Id, messageType, messageHandlerType)) {
                    if (_logger.IsInfoEnabled)
                        _logger.InfoFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                            messageHandlerType.FullName, messageType.FullName, message.Id);
                    return;
                }

                messageHandler.Handle(message);

                if (_logger.IsInfoEnabled)
                    _logger.InfoFormat("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
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

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("message executed. messageId:{0}", message.Id);
            }
        }

        /// <summary>
        /// 执行消息
        /// </summary>
        public virtual void Execute(IMessage message)
        {
            var messageType = message.GetType();
            var messageHandlers = this.GetHandlers(messageType);

            if (messageHandlers.IsEmpty()) {
                var exception = new MessageHandlerNotFoundException(messageType);
                LogManager.GetLogger("ThinkNet").Warn(exception.Message);

                throw exception;
            }

            if (messageType.IsDefined<JustHandleOnceAttribute>(true) && messageHandlers.Count() > 1) {
                var exception = new MessageHandlerTooManyException(messageType);
                LogManager.GetLogger("ThinkNet").Error(exception.Message);

                throw exception;
            }

            foreach (var handler in messageHandlers) {
                ProcessHandler(messageType, message, handler);
            }
        }


        #region IHandlerProvider 成员

        public IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            List<object> handlerlist = new List<object>();

            var handlerType = typeof(IMessageHandler<>).MakeGenericType(type);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType);
            handlerlist.AddRange(handlers);

            if (TypeHelper.IsCommand(type)) {
                handlerType = typeof(ICommandHandler<>).MakeGenericType(type);
                handlers = ServiceLocator.Current.GetAllInstances(handlerType);
                handlerlist.AddRange(handlers);
            }

            if (TypeHelper.IsEvent(type)) {
                handlerType = typeof(IEventHandler<>).MakeGenericType(type);
                handlers = ServiceLocator.Current.GetAllInstances(handlerType);
                handlerlist.AddRange(handlers);
            }


            return handlerlist.Select(handler => {
                var handlerWrapperType = typeof(HandlerWrapper<>).MakeGenericType(type);
                return Activator.CreateInstance(handlerWrapperType, new[] { handler });
            }).OfType<IProxyHandler>().AsEnumerable();
        }

        #endregion
    }
}
