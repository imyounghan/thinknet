using System;
using ThinkLib.Common;

namespace ThinkNet.Messaging.Handling
{
    public class HandlerInterceptionWrapper<T> : DisposableObject, IProxyInterception
        where T : class, IMessage
    {
        private readonly IInterception _interception;
        private readonly Lifecycle _lifetime;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerInterceptionWrapper(IInterception interception)
        {
            this._interception = interception;
            this._lifetime = LifeCycleAttribute.GetLifecycle(interception.GetType());
        }


        public void OnHandlerExecuting(T message)
        {
            if (message.IsNull())
                return;

            var filter = _interception as IMessageInterception<T>;
            if (filter != null)
                filter.OnHandlerExecuting(message);
        }

        public void OnHandlerExecuted(T message, Exception exception)
        {
            if (message.IsNull())
                return;

            var filter = _interception as IMessageInterception<T>;
            if (filter != null)
                filter.OnHandlerExecuted(message, exception);
        }

        protected override void Dispose(bool disposing)
        {
            if (_lifetime != Lifecycle.Singleton && disposing) {
                using (_interception as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }

        public IInterception GetInnerInterception()
        {
            return this._interception;
        }

        #region IProxyInterception 成员
        void IProxyInterception.OnHandlerExecuting(IMessage message)
        {
            this.OnHandlerExecuting(message as T);
        }

        void IProxyInterception.OnHandlerExecuted(IMessage message, Exception exception)
        {
            this.OnHandlerExecuted(message as T, exception);
        }
        #endregion
    }
}
