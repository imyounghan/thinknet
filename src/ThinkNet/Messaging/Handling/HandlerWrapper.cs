using System;
using System.Reflection;
using ThinkNet.Common;
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
        private readonly LifeCycleAttribute.Lifecycle _lifetime;
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

        /// <summary>
        /// dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_lifetime != LifeCycleAttribute.Lifecycle.Singleton && disposing && _handler is IDisposable) {
                ((IDisposable)_handler).Dispose();
            }
        }

        void IProxyHandler.Handle(IMessage message)
        {
            this.Handle(message as T);
        }

        object IProxyHandler.GetInnerHandler()
        {
            return _handler;
        }
    }
}
