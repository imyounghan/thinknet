using System;
using System.Reflection;
using ThinkNet.Annotation;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="IProxyHandler"/> 的包装类
    /// </summary>
    public class HandlerWrapper<T> : IProxyHandler
        where T : class, IMessage
    {
        private readonly IMessageHandler<T> _handler;
        private readonly Lifecycle _lifetime;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerWrapper(IMessageHandler<T> handler)
        {
            this._handler = handler;

            var type = handler.GetType();
            if (type.IsDefined(typeof(LifeCycleAttribute), false)) {
                this._lifetime = type.GetAttribute<LifeCycleAttribute>(false).Lifecycle;
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

            _handler.Handle(message);

            //if (interceptor != null)
            //    interceptor.OnExecuted(message);
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
