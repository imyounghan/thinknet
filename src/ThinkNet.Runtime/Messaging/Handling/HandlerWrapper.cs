using System;
using System.Reflection;
using System.Threading;
using ThinkLib.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;


namespace ThinkNet.Messaging.Handling
{
    public class HandlerWrapper : DisposableObject, IProxyHandler
    {

        class CommandContext : ICommandContext
        {

            private readonly Func<object, IAggregateRoot> provider;

            #region ICommandContext 成员

            public void Add(IAggregateRoot aggregateRoot)
            {
                throw new NotImplementedException();
            }

            public T Get<T>(object id) where T : class, IAggregateRoot
            {
                throw new NotImplementedException();
            }

            public T Find<T>(object id) where T : class, IAggregateRoot
            {
                throw new NotImplementedException();
            }

            public void PendingEvent(IEvent @event)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        //class EmptyInterceptor : IInterceptor<T>
        //{
        //    public static readonly EmptyInterceptor Instance = new EmptyInterceptor();

        //    public void OnHandling(T message)
        //    { }


        //    public void OnHandled(AggregateException exception, T message)
        //    { }
        //}

        private readonly IHandler _handler;
        private readonly Lifecycle _lifetime;
        private readonly IEventSourcedRepository _repository;
        private readonly IEventBus _eventBus;

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
        public void Handle(object message)
        {
            //var interceptor = _handler as IInterceptor<T> ?? EmptyInterceptor.Instance;

            //interceptor.OnHandling(message);

            var handlerType = _handler.GetType();
            if (TypeHelper.IsCommandHandlerType(handlerType)) {
                ((dynamic)_handler).Handle((dynamic)message);
                return;
            }

            if (TypeHelper.IsEventHandlerType(handlerType)) {
                ((dynamic)_handler).Handle((dynamic)message);
                return;
            }

            ((dynamic)_handler).Handle((dynamic)message);

            //var commandHandler = _handler as ICommandHandler<T>;

            //interceptor.OnHandled(null, message);
        }

        //private void RetryHandle(T message)
        //{
        //    try {
        //        this.Handle(message);
        //        return;
        //    }
        //    catch (ThinkNetException) {
        //        throw;
        //    }
        //    catch (Exception) {
        //        if (retryTimes < 3) {
        //            Thread.Sleep(1000);
        //            RetryHandle(message);
        //        }
        //        throw;
        //    }
        //}

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
            this.Handle(message);
        }

        IHandler IProxyHandler.GetInnerHandler()
        {
            return this._handler;
        }
    }


    ///// <summary>
    ///// <see cref="IProxyHandler"/> 的包装类
    ///// </summary>
    //public class HandlerWrapper<T> : DisposableObject, IProxyHandler
    //    where T : class, IMessage
    //{
    //    //class EmptyInterceptor : IInterceptor<T>
    //    //{
    //    //    public static readonly EmptyInterceptor Instance = new EmptyInterceptor();

    //    //    public void OnHandling(T message)
    //    //    { }


    //    //    public void OnHandled(AggregateException exception, T message)
    //    //    { }
    //    //}

    //    private readonly IHandler _handler;
    //    private readonly Lifecycle _lifetime;

    //    private int retryTimes = 0;
    //    /// <summary>
    //    /// Parameterized Constructor.
    //    /// </summary>
    //    public HandlerWrapper(IHandler handler)
    //    {
    //        this._handler = handler;

    //        var type = handler.GetType();
    //        if (type.IsDefined(typeof(LifeCycleAttribute), false)) {
    //            this._lifetime = type.GetAttribute<LifeCycleAttribute>(false).Lifetime;
    //        }
    //    }

    //    /// <summary>
    //    /// Handles the given message with the provided context.
    //    /// </summary>
    //    public void Handle(T message)
    //    {
    //        //var interceptor = _handler as IInterceptor<T> ?? EmptyInterceptor.Instance;

    //        //interceptor.OnHandling(message);

    //        var messageHandler = _handler as IHandler<T>;
    //        if (messageHandler != null) {
    //            messageHandler.Handle(message);
    //        }

    //        //var commandHandler = _handler as ICommandHandler<T>;

    //        //interceptor.OnHandled(null, message);
    //    }

    //    private void RetryHandle(T message)
    //    {
    //        try {
    //            this.Handle(message);
    //            return;
    //        }
    //        catch (ThinkNetException) {
    //            throw;
    //        }
    //        catch (Exception) {
    //            if (retryTimes < 3){
    //                Thread.Sleep(1000);
    //                RetryHandle(message);
    //            }
    //            throw;
    //        }
    //    }

    //    /// <summary>
    //    /// dispose
    //    /// </summary>
    //    protected override void Dispose(bool disposing)
    //    {
    //        if (_lifetime != Lifecycle.Singleton && disposing) {
    //            using (_handler as IDisposable) {
    //                // Dispose handler if it's disposable.
    //            }
    //        }
    //    }

    //    void IProxyHandler.Handle(IMessage message)
    //    {
    //        this.Handle(message as T);
    //    }

    //    IHandler IProxyHandler.GetInnerHandler()
    //    {
    //        return this._handler;
    //    }
    //}
}
