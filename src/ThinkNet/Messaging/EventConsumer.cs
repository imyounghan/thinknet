

namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;


    public class EventConsumer : MessageConsumer<IEnumerable<IEvent>>, IInitializer
    {
        class EventDescriptor
        {
            public static EventDescriptor Create(IEvent @event)
            {
                return new EventDescriptor() {
                    Event = @event,
                    EventType = @event.GetType()
                };
            }

            public IEvent Event { get; set; }

            public Type EventType { get; set; }
        }

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
                return types.OrderBy(type => type.FullName)
                    .Select(type => type.GetHashCode())
                    .Aggregate((x, y) => x ^ y);
            }
        }

        private readonly Dictionary<CompositeKey, Type> _eventTypesMapContractType;
        private readonly ConcurrentDictionary<Type, IEventHandler> _cachedHandlers;
        private readonly IEventPublishedVersionStore _publishedVersionStore;
        private readonly ICommandBus _commandBus;
        private readonly IMessageBus<IEvent> _eventBus;
        private readonly ISendReplyService _sendReplyService;

        public EventConsumer(IMessageBus<PublishableException> exceptionBus,
            ISendReplyService sendReplyService,
            ICommandBus commandBus,
            IMessageBus<IEvent> eventBus,
            ILoggerFactory loggerFactory,
            IMessageReceiver<Envelope<IEnumerable<IEvent>>> eventReceiver)
            : base(eventReceiver, loggerFactory.GetDefault(), "EventStream")
        {
            this._commandBus = commandBus;
            this._eventBus = eventBus;
        }

        protected override void OnMessageReceived(object sender, Envelope<IEnumerable<IEvent>> envelope)
        {
            //var traceInfo = new TraceInfo(
            //    Convert.ToString(envelope.Metadata["processId"]),
            //    Convert.ToString(envelope.Metadata["replyAddress"]));

            //var sourceInfo = new SourceKey(
            //    Convert.ToString(envelope.Metadata["sourceId"]),
            //    Convert.ToString(envelope.Metadata["sourceNamespace"]),
            //    Convert.ToString(envelope.Metadata["sourceTypeName"]),
            //    Convert.ToString(envelope.Metadata["sourceAssemblyName"]));

            var sourceKey = (SourceKey)envelope.Items["SourceKey"];
            var traceInfo = (TraceInfo)envelope.Items["TraceInfo"];
            var version = Convert.ToInt32(envelope.Items["version"]);

            if(version > 1) {
                var lastPublishedVersion = _publishedVersionStore.GetPublishedVersion(sourceKey) + 1;
                if(lastPublishedVersion < version) {
                    //_eventBus.Publish(envelope.Body, sourceKey, version, );
                    if(logger.IsDebugEnabled) {
                        logger.DebugFormat("The event cannot be process now as the version is not the next version, it will be handle later. aggregateRootType={0},aggregateRootId={1},lastPublishedVersion={2},eventVersion={3}",
                            sourceKey.GetSourceTypeName(),
                            sourceKey.Id,
                            lastPublishedVersion,
                            version);
                    }
                    return;
                }

                if(lastPublishedVersion > version) {
                    if(logger.IsDebugEnabled) {
                        logger.DebugFormat("The event is ignored because it is obsoleted. aggregateRootType={0},aggregateRootId={1},lastPublishedVersion={2},eventVersion={3}",
                            sourceKey.GetSourceTypeName(),
                            sourceKey.Id,
                            lastPublishedVersion,
                            version);
                    }
                    return;
                }
            }

            IEventContext eventContext = new EventContext(this._commandBus, this._sendReplyService)
                                             {
                                                 SourceInfo = sourceKey,
                                                 TraceInfo = traceInfo,
                                                 Version = version
                                             };

            this.ProcessingEvents(envelope.Body.Select(EventDescriptor.Create).ToArray(), eventContext);

            this._publishedVersionStore.AddOrUpdatePublishedVersion(sourceKey, version);


            var events =
                envelope.Body.Select(@event => BuildEnvelopedEvent(@event, sourceKey.Id, envelope.CorrelationId))
                    .ToArray();
            this._eventBus.Send(events);
        }

        private static Envelope<IEvent> BuildEnvelopedEvent(IEvent @event, string aggregateRootId, string commandId)
        {
            var envelope = new Envelope<IEvent>(@event);
            envelope.CorrelationId = aggregateRootId;
            envelope.MessageId = ObjectId.GenerateNewStringId();
            envelope.Items["CommandId"] = commandId;

            return envelope;
        }

        private void ProcessingEvents(EventDescriptor[] events, IEventContext eventContext)
        {
            Type[] eventTypes = events.Select(p => p.EventType).ToArray();
            Type eventHandlerType;
            IEventHandler eventHandler = this.GetEventHandler(eventTypes, out eventHandlerType);

            Type[] parameterTypes = eventHandlerType.GetGenericArguments();
            object[] parameters = this.GetParameters(eventContext, events, parameterTypes);

            if (parameters.Length == 1)
            {
                throw new SystemException();
            }

            if(parameters.Length - 1 != events.Length) {
                throw new SystemException();
            }

            TryMultipleInvokeHandlerMethod(eventHandler, parameters);

            eventContext.ActAs<IUnitOfWork>().Commit();
        }

        void TryMultipleInvokeHandlerMethod(IEventHandler eventHandler, object[] parameters, int retryTimes = 5, int retryInterval = 1000)
        {
            int count = 0;

            var messages = string.Join(", ", parameters.Skip(1).Select(p => p.ToString()));

            while(count++ < retryTimes) {
                try
                {
                    TryInvokeHandlerMethod(eventHandler, parameters);
                    break;
                }
                catch(Exception ex) {
                    if(count == retryTimes) {
                        if(logger.IsErrorEnabled) {
                            logger.Error(ex, "Exception raised when handling '{0}' on '{1}'.", messages, eventHandler.GetType().FullName);
                        }
                        return;
                    }
                    if(logger.IsWarnEnabled) {
                        logger.Warn(ex,
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             messages, eventHandler.GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if(logger.IsDebugEnabled) {
                logger.DebugFormat("Handle '{0}' on '{1}' successfully.",
                    messages, eventHandler.GetType().FullName);
            }

        }

        void TryInvokeHandlerMethod(IEventHandler eventHandler, object[] parameters)
        {
            switch(parameters.Length - 1) {
                case 1:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1]);
                    break;
                case 2:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1], (dynamic)parameters[2]);
                    break;
                case 3:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1], (dynamic)parameters[2], (dynamic)parameters[3]);
                    break;
                case 4:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1], (dynamic)parameters[2], (dynamic)parameters[3], (dynamic)parameters[4]);
                    break;
                case 5:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1], (dynamic)parameters[2], (dynamic)parameters[3], (dynamic)parameters[4], (dynamic)parameters[5]);
                    break;
            }
        }

        Type GetEventHandlerInterfaceType(Type[] eventTypes)
        {
            switch(eventTypes.Length) {
                case 0:
                    throw new ArgumentNullException("eventTypes", "An empty array.");
                case 1:
                    return typeof(IEventHandler<>).MakeGenericType(eventTypes[0]);
                default:
                    return _eventTypesMapContractType[new CompositeKey(eventTypes)];
            }
        }

        IEventHandler GetEventHandler(Type[] eventTypes, out Type eventHandlerType)
        {
            eventHandlerType = GetEventHandlerInterfaceType(eventTypes);

            IEventHandler cachedHandler;
            if(_cachedHandlers.TryGetValue(eventHandlerType, out cachedHandler))
                return cachedHandler;

            throw new HandlerNotFoundException(eventTypes);
        }

        private object[] GetParameters(
            IEventContext eventContext,
            IEnumerable<EventDescriptor> events,
            IEnumerable<Type> parameterTypes)
        {
            var array = new ArrayList();
            array.Add(eventContext);

            foreach (var parameterType in parameterTypes)
            {
                var descriptor = events.FirstOrDefault(p => p.EventType == parameterType);
                if (descriptor != null)
                {
                    array.Add(descriptor.Event);
                }
            }

            return array.ToArray();
        }
        
        #region IInitializer 成员
        static bool FilterType(Type type)
        {
            if(!type.IsInterface)
                return false;

            return type.GetInterfaces().Any(FilterInterfaceType);
        }

        static bool FilterInterfaceType(Type type)
        {
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>)/* ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>)*/;
        }

        public override void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var eventHandlerInterfaceTypes = assemblies
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(FilterType)
                .SelectMany(type => type.GetInterfaces())
                .Where(FilterInterfaceType);

            foreach(var interfaceType in eventHandlerInterfaceTypes) {
                var genericTypes = interfaceType.GetGenericArguments();
                var key = new CompositeKey(genericTypes);

                if(_eventTypesMapContractType.ContainsKey(key)) {
                    string errorMessage = string.Format("There are have duplicate IEventHandler interface type for '{0}'.",
                        string.Join(",", key.Select(item => item.FullName)));
                    throw new SystemException(errorMessage);
                }

                _eventTypesMapContractType[key] = interfaceType;
            }

            foreach(var eventHandlerType in _eventTypesMapContractType.Values) {
                var eventHandlers = container.ResolveAll(eventHandlerType).OfType<IEventHandler>().ToList();
                switch(eventHandlers.Count) {
                    case 0:
                        break;
                    case 1:
                        _cachedHandlers[eventHandlerType] = eventHandlers.First();
                        break;
                    default:
                        throw new SystemException(string.Format("Found more than one event handler for '{0}' with IEventHandler<>.", eventHandlerType.GetGenericArguments().Select(item => item.FullName)));
                }
            }
        }

        #endregion
    }

    //public class EventConsumer : Processor, IInitializer
    //{

    //    private readonly Dictionary<Type, ICollection<IHandler>> _handlers;
    //    private readonly Dictionary<Type, ICollection<IHandler>> _envelopeHandlers;
    //    private readonly ILogger _logger;
    //    private readonly IMessageReceiver<Envelope<IEvent>> _receiver;

    //    public EventConsumer(ILoggerFactory loggerFactory,
    //        IMessageReceiver<Envelope<IEvent>> eventReceiver)
    //    {
    //        this._logger = loggerFactory.GetDefault();
    //        this._receiver = eventReceiver;

    //        this._envelopeHandlers = new Dictionary<Type, ICollection<IHandler>>();
    //        this._handlers = new Dictionary<Type, ICollection<IHandler>>();
    //    }

    //    /// <summary>
    //    /// 启动进程
    //    /// </summary>
    //    protected override void Start()
    //    {
    //        this._receiver.MessageReceived += this.OnEventReceived;
    //        this._receiver.Start();

    //        Console.WriteLine("Event Consumer Started!");
    //    }

    //    /// <summary>
    //    /// 停止进程
    //    /// </summary>
    //    protected override void Stop()
    //    {
    //        this._receiver.MessageReceived -= this.OnEventReceived;
    //        this._receiver.Stop();

    //        Console.WriteLine("Event Consumer Stopped!");
    //    }


        

    //    private void OnEventReceived(object sender, Envelope<IEvent> @event)
    //    {
    //        EventDescriptor eventDescriptor = new EventDescriptor()
    //                                              {
    //                                                  AggregateRootId = @event.CorrelationId,
    //                                                  Event = @event.Body,
    //                                                  EventType = @event.Body.GetType(),
    //                                                  Id = @event.MessageId
    //                                              };
    //        List<IHandler> combinedHandlers = new List<IHandler>();
    //        if (_handlers.ContainsKey(eventDescriptor.EventType))
    //        {
    //            combinedHandlers.AddRange(_handlers[eventDescriptor.EventType]);
    //        }
    //        if(_envelopeHandlers.ContainsKey(eventDescriptor.EventType)) {
    //            combinedHandlers.AddRange(_envelopeHandlers[eventDescriptor.EventType]);
    //        }
    //        eventDescriptor.EventHandlers = combinedHandlers;

    //        eventDescriptor.Execute(_logger);
    //    }

    //    #region IInitializer 成员

    //    public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
    //    {
    //        var eventTypes =
    //            assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    //                .Where(type => type.IsAssignableFrom(typeof(IEvent)))
    //                .ToArray();

    //        foreach(var eventType in eventTypes)
    //        {
    //            var envelopedEventHandlers =
    //                container.ResolveAll(typeof(IEnvelopedMessageHandler<>).MakeGenericType(eventType))
    //                    .OfType<IEnvelopeHandler>()
    //                    .Cast<IHandler>()
    //                    .ToList();

    //            if (envelopedEventHandlers.Count > 0)
    //            {
    //                _envelopeHandlers[eventType] = envelopedEventHandlers;
    //            }

    //            var handlers =
    //                container.ResolveAll(typeof(IMessageHandler<>).MakeGenericType(eventType))
    //                    .OfType<IHandler>()
    //                    .ToList();

    //            if (handlers.Count > 0)
    //            {
    //                _handlers[eventType] = handlers;
    //            }
    //        }
    //    }

    //    #endregion
    //}
}
