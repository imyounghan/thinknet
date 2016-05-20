using System;
using ThinkLib.Common;

namespace ThinkNet.Messaging.Handling
{
    public class MessageHandlerWrapper<T> : DisposableObject, IProxyHandler
        where T : class, IMessage
    {
        private readonly IHandler _handler;
        private readonly Lifecycle _lifetime;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageHandlerWrapper(IHandler handler)
        {
            this._handler = handler;
            this._lifetime = LifeCycleAttribute.GetLifecycle(_handler.GetType());
        }

        /// <summary>
        /// Handles the given message with the provided context.
        /// </summary>
        public virtual void Handle(T message)
        {
            var handler = _handler as IMessageHandler<T>;
            if (handler != null) {
                handler.Handle(message);
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

        public IHandler GetInnerHandler()
        {
            return this._handler;
        }

        void IProxyHandler.Handle(IMessage message)
        {
            this.Handle(message as T);
        }        
    }
}
