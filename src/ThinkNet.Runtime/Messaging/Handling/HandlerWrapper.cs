using System;
using ThinkNet.Infrastructure;
using ThinkLib.Common;


namespace ThinkNet.Messaging.Handling
{
    public class HandlerWrapper : DisposableObject, IProxyHandler
    {
        private readonly IHandler _handler;
        private readonly Lifecycle _lifetime;
        private readonly Type _handlerType;
        private readonly ICommandContextFactory _commandContextFactory;
        private readonly IEventContextFactory _eventContextFactory;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerWrapper(IHandler handler)
        {
            this._handler = handler;
            this._handlerType = handler.GetType();
            this._lifetime = LifeCycleAttribute.GetLifecycle(_handlerType);
        }

        /// <summary>
        /// Handles the given message with the provided context.
        /// </summary>
        public void Handle(object message)
        {
            if (TypeHelper.IsCommandHandlerType(_handlerType)) {
                var context = _commandContextFactory.CreateCommandContext();
                ((dynamic)_handler).Handle(context, (dynamic)message);
                context.Commit();
                return;
            }

            if (TypeHelper.IsEventHandlerType(_handlerType)) {
                var context = _eventContextFactory.CreateEventContext();
                ((dynamic)_handler).Handle(context, (dynamic)message);
                context.Commit();
                return;
            }

            ((dynamic)_handler).Handle((dynamic)message);
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
            this.Handle(message);
        }

        IHandler IProxyHandler.GetInnerHandler()
        {
            return this._handler;
        }
    }

    public class HandlerWrapper<T> : DisposableObject, IProxyHandler
        where T : class, IMessage
    {
        class CommandHandlerWrapper<TCommand> where TCommand : class, ICommand
        {
            private readonly ICommandHandler<TCommand> commandHandler;
            private readonly ICommandContextFactory commandContextFactory;

            public void Handle(TCommand command)
            {
                var context = commandContextFactory.CreateCommandContext();
                commandHandler.Handle(context, command);
                context.Commit();
            }
        }

        class EventHandlerWrapper<TEvent> where TEvent : class, IEvent
        {
            private readonly IEventHandler<TEvent> eventHandler;
            private readonly IEventContextFactory eventContextFactory;

            public void Handle(TEvent @event)
            {
                var context = eventContextFactory.CreateEventContext();
                eventHandler.Handle(context, @event);
                context.Commit();
            }
        }


        private readonly IHandler _handler;
        private readonly Lifecycle _lifetime;
        private readonly Type _handlerType;
        private readonly ICommandContextFactory _commandContextFactory;
        private readonly IEventContextFactory _eventContextFactory;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerWrapper(IHandler handler)
        {
            this._handler = handler;
            this._handlerType = handler.GetType();
            this._lifetime = LifeCycleAttribute.GetLifecycle(_handlerType);
        }

        /// <summary>
        /// Handles the given message with the provided context.
        /// </summary>
        public void Handle(T message)
        {
            if (TypeHelper.IsCommandHandlerType(_handlerType)) {
                var context = _commandContextFactory.CreateCommandContext();
                ((dynamic)_handler).Handle(context, (dynamic)message);
                context.Commit();
                return;
            }

            if (TypeHelper.IsEventHandlerType(_handlerType)) {
                var context = _eventContextFactory.CreateEventContext();
                ((dynamic)_handler).Handle(context, (dynamic)message);
                context.Commit();
                return;
            }

            ((dynamic)_handler).Handle((dynamic)message);
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
