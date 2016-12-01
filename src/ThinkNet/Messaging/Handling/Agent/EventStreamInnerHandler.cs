using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// <see cref="EventStream"/> 的内部处理程序
    /// </summary>
    public class EventStreamInnerHandler : IHandlerAgent, IInitializer//, IMessageHandler<EventStream>
    {
        private readonly static Dictionary<CompositeKey, Type> EventTypesMapContractType = new Dictionary<CompositeKey, Type>();
        private readonly static Type EventStreamType = typeof(EventStream);
        private readonly static Type EventStreamHandlerType = typeof(EventStreamInnerHandler);

        private readonly ConcurrentDictionary<Type, IHandlerAgent> _cachedHandlers;
        private readonly IObjectContainer _container;
        private readonly IMessageBus _messageBus;
        private readonly IMessageHandlerRecordStore _handlerStore;
        private readonly IEventPublishedVersionStore _publishedVersionStore;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventStreamInnerHandler(IObjectContainer container, 
            IMessageBus messageBus,
            IMessageHandlerRecordStore handlerStore,
            IEventPublishedVersionStore publishedVersionStore)            
        {
            this._container = container;           
            this._cachedHandlers = new ConcurrentDictionary<Type, IHandlerAgent>();
            this._messageBus = messageBus;
            this._handlerStore = handlerStore;
            this._publishedVersionStore = publishedVersionStore;
        }
        

        public object GetInnerHandler()
        {
            return this;
        }

        public void Handle(params object[] args)
        {
            var stream = args[0] as EventStream;
            if(stream.Events.IsEmpty()) {
                _messageBus.Publish(new CommandResult(stream.CorrelationId, CommandReturnType.DomainEventHandled, CommandStatus.NothingChanged));
                return;
            }

            if(_handlerStore.HandlerIsExecuted(stream.CorrelationId, EventStreamType, EventStreamHandlerType)) {
                var errorMessage = string.Format("The EventStream has been handled, Data({0}).", stream);
                _messageBus.Publish(new CommandResult(stream.CorrelationId, new ThinkNetException(errorMessage)));
                if(LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.Warn(errorMessage);
                }
                return;
            }

            try {
                this.TryHandle(stream);
                _messageBus.Publish(new CommandResult(stream.CorrelationId, CommandReturnType.DomainEventHandled));
            }
            catch(Exception ex) {
                _messageBus.Publish(new CommandResult(stream.CorrelationId, ex, CommandReturnType.DomainEventHandled));
                throw ex;
            }
           

            _handlerStore.AddHandlerInfo(stream.CorrelationId, EventStreamType, EventStreamHandlerType);
        }

        private void TryHandle(EventStream @event)
        {
            if(@event.Version > 1) {
                var version = _publishedVersionStore.GetPublishedVersion(@event.SourceId) + 1;
                if(version < @event.Version) {
                    _messageBus.Publish(@event);
                    throw new DomainEventAsPendingException() {
                        RelatedId = @event.SourceId.Id,
                        RelatedType = @event.SourceId.GetSourceTypeFullName()
                    };
                }
                else if(version > @event.Version) {
                    throw new DomainEventObsoletedException() {
                        RelatedId = @event.SourceId.Id,
                        RelatedType = @event.SourceId.GetSourceTypeFullName(),
                        Version = @event.Version
                    };
                }
            }

            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            var eventHandler = this.GetEventHandler(eventTypes);
            var parameters = this.GetParameters(@event);

            eventHandler.Handle(parameters);

            _messageBus.Publish(@event.Events);

            _publishedVersionStore.AddOrUpdatePublishedVersion(@event.SourceId, @event.Version);
        }

        private IHandlerAgent BuildEventHandler(IHandler handler, Type eventHandlerType)
        {
            var eventTypes = eventHandlerType.GetGenericArguments();
            Type contractType;
            //Type eventHandlerAgentType;
            switch (eventTypes.Length) {
                case 1:
                    //eventHandlerAgentType = typeof(EventHandlerAgent<>).MakeGenericType(eventTypes);
                    contractType = typeof(IEventHandler<>).MakeGenericType(eventTypes);
                    break;
                case 2:
                    //eventHandlerAgentType = typeof(EventHandlerAgent<,>).MakeGenericType(eventTypes);
                    contractType = typeof(IEventHandler<,>).MakeGenericType(eventTypes);
                    break;
                case 3:
                    //eventHandlerAgentType = typeof(EventHandlerAgent<,,>).MakeGenericType(eventTypes);
                    contractType = typeof(IEventHandler<,,>).MakeGenericType(eventTypes);
                    break;
                case 4:
                    //eventHandlerAgentType = typeof(EventHandlerAgent<,,,>).MakeGenericType(eventTypes);
                    contractType = typeof(IEventHandler<,,,>).MakeGenericType(eventTypes);
                    break;
                case 5:
                    //eventHandlerAgentType = typeof(EventHandlerAgent<,,,,>).MakeGenericType(eventTypes);
                    contractType = typeof(IEventHandler<,,,,>).MakeGenericType(eventTypes);
                    break;
                default:
                    throw new ThinkNetException();
            }

            //return (IHandlerAgent)Activator.CreateInstance(eventHandlerAgentType, handler);
            //var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
            //return new EventHandlerAgent(handler, method);

            return new EventHandlerAgent(contractType, handler);
        }

        /// <summary>
        /// 获取事件对应的处理器接口类型
        /// </summary>
        private static Type GetEventHandlerInterfaceType(Type[] eventTypes)
        {
            switch (eventTypes.Length) {
                case 0:
                    throw new ArgumentNullException("eventTypes", "An empty array.");
                case 1:
                    return typeof(IEventHandler<>).MakeGenericType(eventTypes[0]);
                default:
                    return EventTypesMapContractType[new CompositeKey(eventTypes)];
            }
        }

        /// <summary>
        /// 获取处理事件的代理
        /// </summary>
        protected IHandlerAgent GetEventHandler(Type[] types)
        {
            var contractType = GetEventHandlerInterfaceType(types);

            IHandlerAgent cachedHandler;
            if(_cachedHandlers.TryGetValue(contractType, out cachedHandler))
                return cachedHandler;

            var handlers = _container.ResolveAll(contractType).Cast<IHandler>()
                .Select(handler => BuildEventHandler(handler, contractType))
                .ToArray();

            switch(handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(types);
                case 1:
                    var handler = handlers[0];
                    var lifecycle = LifeCycleAttribute.GetLifecycle(handler.GetInnerHandler().GetType());
                    if(lifecycle == Lifecycle.Singleton)
                        _cachedHandlers.TryAdd(contractType, handler);
                    return handler;
                default:
                    throw new MessageHandlerTooManyException(types);
            }
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        protected object[] GetParameters(EventStream eventStream)
        {
            var array = new ArrayList();
            array.Add(new SourceMetadata {
                CorrelationId = eventStream.CorrelationId,
                SourceId = eventStream.SourceId.Id,
                SourceTypeName = eventStream.SourceId.GetSourceTypeFullName(),
                Version = eventStream.Version
            });

            var collection = eventStream.Events as ICollection;
            if (collection != null) {
                array.AddRange(collection);
            }
            else {
                foreach (var el in eventStream.Events)
                    array.Add(el);
            }

            return array.ToArray();
        }

        private static bool FilterType(Type type)
        {
            if(!type.IsInterface)
                return false;

            return type.GetInterfaces().Any(FilterInterfaceType);
        }

        private static bool FilterInterfaceType(Type type)
        {
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        } 

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var eventHandlerInterfaceTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(FilterType)
                .SelectMany(type => type.GetInterfaces())
                .Where(FilterInterfaceType);

            foreach (var interfaceType in eventHandlerInterfaceTypes) {
                var genericTypes = interfaceType.GetGenericArguments();
                var key = new CompositeKey(genericTypes);

                EventTypesMapContractType.TryAdd(key, interfaceType);
                if(EventTypesMapContractType.ContainsKey(key)) {
                    string errorMessage = string.Format("There are have duplicate IEventHandler interface type for {0}.",
                        string.Join(",", key.Select(item => item.FullName)));
                    throw new ThinkNetException(errorMessage);
                }

                EventTypesMapContractType[key] = interfaceType;
            }
        }



        //#region IMessageHandler<EventStream> 成员

        //void IMessageHandler<EventStream>.Handle(EventStream eventStream)
        //{
        //    this.TryHandle(eventStream);
        //}

        //#endregion

        #region
        struct CompositeKey : IEnumerable<Type>
        {
            private readonly IEnumerable<Type> types;

            public CompositeKey(IEnumerable<Type> types)
            {
                if(types.Distinct().Count() != types.Count()) {
                    throw new ArgumentException("There are have duplicate types.", "types");
                }

                this.types = types;
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return types.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach(var type in types) {
                    yield return type;
                }
            }

            public override bool Equals(object obj)
            {
                if(obj == null || obj.GetType() != this.GetType())
                    return false;
                var other = (CompositeKey)obj;

                return this.Except(other).IsEmpty();
            }

            public override int GetHashCode()
            {
                return types.OrderBy(type => type.FullName).Select(type => type.GetHashCode()).Aggregate((x, y) => x ^ y);
            }
        }
        #endregion
    }
}
