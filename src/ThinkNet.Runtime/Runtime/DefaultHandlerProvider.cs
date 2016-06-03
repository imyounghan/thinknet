using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime
{
    internal class DefaultHandlerProvider : IHandlerProvider, IInitializer
    {
        private readonly ICommandContextFactory _commandContextFactory;
        private readonly IEventContextFactory _eventContextFactory;

        private readonly Dictionary<Type, IProxyHandler> singleHandlerDict;
        private readonly Dictionary<Type, IEnumerable<IProxyHandler>> manyHandlerDict;

        public DefaultHandlerProvider(ICommandContextFactory commandContextFactory, IEventContextFactory eventContextFactory)
        {
            this._commandContextFactory = commandContextFactory;
            this._eventContextFactory = eventContextFactory;

            this.singleHandlerDict = new Dictionary<Type, IProxyHandler>();
            this.manyHandlerDict = new Dictionary<Type, IEnumerable<IProxyHandler>>();
        }


        public IProxyHandler GetCommandHandler(Type commandType)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                .Cast<IHandler>()
                .Select(handler => new CommandHandlerWrapper(handler, _commandContextFactory))
                .Cast<IProxyHandler>()
                .ToArray();

            if (handlers.IsEmpty()) {
                handlers = this.GetHandlers(commandType).ToArray();
            }


            switch (handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(commandType);
                case 1:
                    return handlers[0];
                default:
                    throw new MessageHandlerTooManyException(commandType);
            }
        }

        public IProxyHandler GetEventHandler(Type eventType)
        {
            var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
            var handlers = ServiceLocator.Current.GetAllInstances(handlerType)
                .Cast<IHandler>()
                .Select(handler => new EventHandlerWrapper(handler, _eventContextFactory))
                .Cast<IProxyHandler>()
                .ToArray();

            switch (handlers.Length) {
                case 0:
                    return null;
                case 1:
                    return handlers[0];
                default:
                    throw new MessageHandlerTooManyException(eventType);
            }
        }

        public IEnumerable<IProxyHandler> GetHandlers(Type messageType)
        {
            var handlerType = typeof(IHandler<>).MakeGenericType(messageType);
            return ServiceLocator.Current.GetAllInstances(handlerType)
                .Cast<IHandler>()
                .Select(handler => new HandlerWrapper(handler))
                .Cast<IProxyHandler>();
        }

        #region IHandlerProvider 成员

        IEnumerable<IProxyHandler> IHandlerProvider.GetHandlers(Type type)
        {
            return manyHandlerDict[type];
        }

        IProxyHandler IHandlerProvider.GetCommandHandler(Type type)
        {
            return singleHandlerDict[type];
        }

        IProxyHandler IHandlerProvider.GetEventHandler(Type type)
        {
            return singleHandlerDict[type];
        }

        #endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            foreach (var type in types) {
                if (TypeHelper.IsCommand(type)) {
                    singleHandlerDict[type] = this.GetCommandHandler(type);
                }
                if (TypeHelper.IsVersionedEvent(type)) {
                    singleHandlerDict[type] = this.GetEventHandler(type);
                }
                if (TypeHelper.IsEvent(type)) {
                    manyHandlerDict[type] = this.GetHandlers(type).ToArray();
                }
            }
        }

        #endregion

        public class HandlerWrapper : DisposableObject, IProxyHandler
        {
            private readonly IHandler handler;
            private readonly Type handerType;
            private readonly Lifecycle lifetime;

            public HandlerWrapper(IHandler handler)
            {
                this.handler = handler;
                this.handerType = handler.GetType();
                this.lifetime = LifeCycleAttribute.GetLifecycle(handerType);
            }

            public virtual void Handle(object handler, object message)
            {
                ((dynamic)handler).Handle((dynamic)message);
            }

            /// <summary>
            /// dispose
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                if (lifetime != Lifecycle.Singleton && disposing) {
                    using (handler as IDisposable) {
                        // Dispose handler if it's disposable.
                    }
                }
            }

            public Type HanderType { get { return this.handerType; } }

            public IHandler GetInnerHandler()
            {
                return this.handler;
            }

            #region IProxyHandler 成员

            void IProxyHandler.Handle(object message)
            {
                this.Handle(handler, message);
            }
            #endregion
        }

        public class CommandHandlerWrapper : HandlerWrapper
        {
            private readonly ICommandContextFactory commandContextFactory;

            public CommandHandlerWrapper(IHandler handler, ICommandContextFactory commandContextFactory)
                : base(handler)
            {
                this.commandContextFactory = commandContextFactory;
            }

            public override void Handle(object handler, object message)
            {
                var context = commandContextFactory.CreateCommandContext();
                ((dynamic)handler).Handle((dynamic)context, (dynamic)message);
                context.Commit(((dynamic)message).Id);
            }
        }

        public class EventHandlerWrapper : HandlerWrapper
        {
            private readonly IEventContextFactory eventContextFactory;

            public EventHandlerWrapper(IHandler handler, IEventContextFactory eventContextFactory)
                : base(handler)
            {
                this.eventContextFactory = eventContextFactory;
            }

            public override void Handle(object handler, object message)
            {
                var context = eventContextFactory.GetEventContext();
                ((dynamic)handler).Handle(context, (dynamic)message);
            }
        }
    }
}
