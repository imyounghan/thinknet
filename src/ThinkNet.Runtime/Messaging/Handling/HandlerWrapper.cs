using System;
using System.Reflection;
using System.Threading;
using ThinkLib.Common;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="IProxyHandler"/> 的包装类
    /// </summary>
    public class HandlerWrapper<T> : DisposableObject, IProxyHandler
        where T : class, IMessage
    {
        private readonly IHandler _handler;
        private readonly Lifecycle _lifetime;

        private int retryTimes = 0;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerWrapper(IHandler handler)
        {
            this._handler = handler;

            var type = handler.GetType();
            if (type.IsDefined(typeof(LifeCycleAttribute), false)) {
                this._lifetime = type.GetAttribute<LifeCycleAttribute>(false).Lifetime;
            }
        }

        /// <summary>
        /// Handles the given message with the provided context.
        /// </summary>
        public void Handle(T message)
        {
            //var interceptor = _handler as IInterceptor<T>;

            //if (interceptor != null)
            //    interceptor.OnExecuting(message);

            var messageHandler = _handler as IMessageHandler<T>;
            if (messageHandler != null)
            {
                messageHandler.Handle(message);
            }

            //if (interceptor != null)
            //    interceptor.OnExecuted(message);
        }

        private void RetryHandle(T message)
        {
            try {
                this.Handle(message);
                return;
            }
            catch (ThinkNetException) {
                throw;
            }
            catch (Exception) {
                if (retryTimes < 3){
                    Thread.Sleep(1000);
                    RetryHandle(message);
                }
                throw;
            }
        }

        /// <summary>
        /// dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_lifetime != Lifecycle.Singleton && disposing) {
                using (_handler as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }

        void IProxyHandler.Handle(IMessage message)
        {
            this.Handle(message as T);
        }

        IHandler IProxyHandler.GetInnerHandler()
        {
            return this._handler;
        }
    }
}
