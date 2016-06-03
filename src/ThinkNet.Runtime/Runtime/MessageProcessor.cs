using System;
using System.Collections.Generic;
using System.Threading;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Runtime
{
    public abstract class MessageProcessor<T> : Processor<T>, IInitializer
        where T : IMessage
    {
        private readonly IHandlerRecordStore _handlerStore;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageProcessor(IHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
        }


        protected void DuplicateProcessHandler(IProxyHandler handler, IMessage message, Type messageType)
        {
            try {
                handler.Handle(message);

                if (logger.IsDebugEnabled) {
                    var debugMessage = string.Format("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                        handler.HanderType.FullName, messageType.FullName, message.Id);
                    logger.DebugFormat(debugMessage);
                }
            }
            catch (Exception) {
                if (logger.IsErrorEnabled) {
                    var errorMessage = string.Format("Exception raised when {0} handling {1}. message info:{2}.",
                        handler.HanderType.FullName, messageType.FullName, message.Id);
                    logger.Error(errorMessage);
                }
                throw;
            }
        }

        protected void OnlyonceProcessHandler(IProxyHandler handler, IMessage message, Type messageType)
        {
            try {
                if (_handlerStore.HandlerIsExecuted(message.Id, messageType, handler.HanderType)) {
                    if (logger.IsDebugEnabled)
                        logger.DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                             handler.HanderType.FullName, messageType.FullName, message.Id);
                    return;
                }
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Check the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    handler.HanderType.FullName, messageType.FullName, message.Id);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }

            DuplicateProcessHandler(handler, message, messageType);

            try {
                _handlerStore.AddHandlerInfo(message.Id, messageType, handler.HanderType);
            }
            catch (Exception ex) {
                var errorMsg = string.Format("Save the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
                    handler.HanderType.FullName, messageType.FullName, message.Id);
                throw new HandlerRecordStoreException(errorMsg, ex);
            }
        }

        protected abstract void Execute(T message);

        protected abstract void Notify(T message, Exception exception);

        protected override void Process(T message)
        {
            int count = 0;
            int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;


            Exception exception = null;
            while (count++ < retryTimes) {
                try {
                    this.Execute(message);
                    break;
                }
                catch (ThinkNetException ex) {
                    exception = ex;
                    break;
                }
                catch (Exception ex) {
                    if (count == retryTimes) {
                        exception = ex;
                        break;
                    }
                    if (logger.IsWarnEnabled) {
                        logger.Warn("retry.", ex);
                    }
                    Thread.Sleep(ConfigurationSetting.Current.HandleRetryInterval);
                }
            }

            this.Notify(message, exception);
            if (exception != null)
                throw exception;
        }

        #region IInitializer 成员
        public void Initialize(IEnumerable<Type> types)
        {
            this.Start();
        }

        #endregion        
    }
}
