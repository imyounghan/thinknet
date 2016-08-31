using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Configurations;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    internal class DefaultHandlerProvider : IHandlerProvider, IInitializer
    {
        class CompositeKey : IEnumerable<Type>
        {
            private readonly IEnumerable<Type> eventTypes;

            public CompositeKey(IEnumerable<Type> types)
            {
                this.eventTypes = types;
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return eventTypes.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)eventTypes).GetEnumerator();
            }

            public override bool Equals(object obj)
            {
                var other = obj as CompositeKey;

                if(other == null)
                    return false;

                return this.Except(other).IsEmpty();
            }

            public override int GetHashCode()
            {
                return eventTypes.Select(type => type.GetHashCode()).Aggregate((x, y) => x ^ y);
            }
        }

        private readonly ICommandContextFactory _commandContextFactory;
        //private readonly IEventContextFactory _eventContextFactory;

        private readonly Dictionary<CompositeKey, Type> EventTypesMapServiceType;
        //private readonly Dictionary<CompositeKey, IProxyHandler> eventHanderDict;
        //private readonly Dictionary<Type, IProxyHandler> commandHandlerDict;

        //private readonly Dictionary<Type, IProxyHandler> singleHandlerDict;
        //private readonly Dictionary<Type, IEnumerable<IProxyHandler>> manyHandlerDict;

        public DefaultHandlerProvider(IEventSourcedRepository repository, IEventBus eventBus)
        {
            this._commandContextFactory = new CommandContextFactory(repository, eventBus);
            this.EventTypesMapServiceType = new Dictionary<CompositeKey, Type>();
            //this._eventContextFactory = eventContextFactory;

            //this.singleHandlerDict = new Dictionary<Type, IProxyHandler>();
            //this.manyHandlerDict = new Dictionary<Type, IEnumerable<IProxyHandler>>();
        }


        public IProxyHandler GetCommandHandler(Type commandType)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
            var handlers = ObjectContainer.Instance.ResolveAll(handlerType)
                .Cast<IHandler>()
                .Select(handler => new CommandHandlerWrapper(handler, _commandContextFactory))
                .Cast<IProxyHandler>()
                .ToArray();

            if (handlers.IsEmpty()) {
                handlers = this.GetMessageHandlers(commandType).ToArray();
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

        public IProxyHandler GetEventHandler(IEnumerable<Type> types)
        {
            var handlerType = EventTypesMapServiceType[new CompositeKey(types)];// typeof(IEventHandler<>).MakeGenericType(eventType);
            var handlers = ObjectContainer.Instance.ResolveAll(handlerType)
                .Cast<IHandler>()
                .Select(handler => new EventHandlerWrapper(handler, handlerType))
                .Cast<IProxyHandler>()
                .ToArray();

            switch(handlers.Length) {
                case 0:
                    return null;
                case 1:
                    return handlers[0];
                default:
                    throw new MessageHandlerTooManyException(types);
            }
        }

        //public IProxyHandler GetEventHandler(Type eventType)
        //{
        //    var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        //    var handlers = ObjectContainer.Instance.ResolveAll(handlerType)
        //        .Cast<IHandler>()
        //        .Select(handler => new EventHandlerWrapper(handler, _eventContextFactory))
        //        .Cast<IProxyHandler>()
        //        .ToArray();

        //    switch (handlers.Length) {
        //        case 0:
        //            return null;
        //        case 1:
        //            return handlers[0];
        //        default:
        //            throw new MessageHandlerTooManyException(eventType);
        //    }
        //}

        public IEnumerable<IProxyHandler> GetMessageHandlers(Type type)
        {
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(type);
            return ObjectContainer.Instance.ResolveAll(handlerType)
                .Cast<IHandler>()
                .Select(handler => new MessageHandlerWrapper(handler))
                .Cast<IProxyHandler>();
        }

        //#region IHandlerProvider 成员

        //IEnumerable<IProxyHandler> IHandlerProvider.GetHandlers(Type type)
        //{
        //    return manyHandlerDict[type];
        //}

        //IProxyHandler IHandlerProvider.GetCommandHandler(Type type)
        //{
        //    return singleHandlerDict[type];
        //}

        //IProxyHandler IHandlerProvider.GetEventHandler(Type type)
        //{
        //    return singleHandlerDict[type];
        //}

        //#endregion

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            EventSourcedInnerHandlerProvider.Initialize(types);

            foreach(var type in types.Where(p => p.IsClass && !p.IsAbstract)) {
                var interfaces = type.GetInterfaces();
                if(interfaces == null || interfaces.Length == 0)
                    continue;
                
                foreach(var interfaceType in interfaces.Where(TypeHelper.IsEventHandlerInterfaceType)) {
                    var eventTypes = new CompositeKey(interfaceType.GenericTypeArguments);
                    if(EventTypesMapServiceType.ContainsKey(eventTypes)) {

                    }
                    else {
                        EventTypesMapServiceType[eventTypes] = interfaceType;
                    }
                }
            }
            //foreach (var type in types.Where(TypeHelper.IsHandlerType)) {
            //    foreach(var interfaceType in type.GetInterfaces()) {
            //        if(TypeHelper.IsCommandHandlerInterfaceType(interfaceType)) {
            //            var commandType = type.GenericTypeArguments[0];
            //            commandHandlerDict[commandType] = this.GetCommandHandler(commandType);
            //        }
            //        else if(TypeHelper.IsEventHandlerInterfaceType(type)) {
            //            var eventTypes = new CompositeKey(type.GenericTypeArguments);
            //            eventHanderTypeMap[eventTypes] = interfaceType;
            //            //eventHanderDict[eventTypes] = 
            //        }
            //    }
            //    if (TypeHelper.IsCommand(type)) {
            //        singleHandlerDict[type] = this.GetCommandHandler(type);
            //    }
            //    if (TypeHelper.IsEvent(type)) {
            //        //singleHandlerDict[type] = this.GetEventHandler(type);
            //        manyHandlerDict[type] = this.GetHandlers(type).ToArray();
            //    }
            //    if(TypeHelper.IsEventHandlerType(type)) {
            //        type.GetInterfaces().Where(TypeHelper.IsEventHandlerInterfaceType);
            //    }
            //}
        }

        #endregion

        public class MessageHandlerWrapper : DisposableObject, IProxyHandler
        {
            private readonly IHandler handler;

            public MessageHandlerWrapper(IHandler handler)
            {
                this.handler = handler;
                this.HanderType = handler.GetType();
            }

            public virtual void Handle(object handler, object[] args)
            {
                var message = args.First();
                ((dynamic)handler).Handle((dynamic)message);
            }

            /// <summary>
            /// dispose
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                if (LifeCycleAttribute.GetLifecycle(this.HanderType) == Lifecycle.Transient && disposing) {
                    using (handler as IDisposable) {
                        // Dispose handler if it's disposable.
                    }
                }
            }

            public Type HanderType { get; private set; }

            public IHandler GetInnerHandler()
            {
                return this.handler;
            }

            #region IProxyHandler 成员

            void IProxyHandler.Handle(params object[] args)
            {
                this.Handle(handler, args);
            }
            #endregion
        }

        public class CommandHandlerWrapper : MessageHandlerWrapper
        {
            private readonly ICommandContextFactory commandContextFactory;

            public CommandHandlerWrapper(IHandler handler, ICommandContextFactory commandContextFactory)
                : base(handler)
            {
                this.commandContextFactory = commandContextFactory;
            }

            public override void Handle(object handler, object[] args)
            {
                var context = commandContextFactory.CreateCommandContext();
                var message = args.First();

                ((dynamic)handler).Handle((dynamic)context, (dynamic)message);
                context.Commit(((dynamic)message).Id);
            }
        }

        public class EventHandlerWrapper : MessageHandlerWrapper
        {
            public EventHandlerWrapper(IHandler handler, Type serviceType)
                : base(handler)
            {
                this.ServiceType = serviceType;
            }

            public Type ServiceType { get; private set; }

            public override void Handle(object handler, object[] args)
            {
                var version = args.First();
                switch(args.Length - 1) {
                    case 1:
                        ((dynamic)handler).Handle((dynamic)version, (dynamic)args[1]);
                        break;
                    case 2:
                        var event1 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[1]);
                        var event2 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[2]);
                        ((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2);
                        break;
                    case 3:
                        event1 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[1]);
                        event2 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[2]);
                        var event3 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[3]);
                        ((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3);
                        break;
                    case 4:
                        event1 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[1]);
                        event2 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[2]);
                        event3 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[3]);
                        var event4 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[4]);
                        ((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3, (dynamic)event4);
                        break;
                    case 5:
                        event1 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[1]);
                        event2 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[2]);
                        event3 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[3]);
                        event4 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[4]);
                        var event5 = args.First(p => p.GetType() == ServiceType.GenericTypeArguments[5]);
                        ((dynamic)handler).Handle((dynamic)version, (dynamic)event1, (dynamic)event2, (dynamic)event3, (dynamic)event4, (dynamic)event5);
                        break;
                }
            }
        }
    }
}
