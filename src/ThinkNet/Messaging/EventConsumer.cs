
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using ThinkNet.Infrastructure;
    using ThinkNet.Messaging.Handling;

    /// <summary>
    /// The EventCollection consumer.
    /// </summary>
    public class EventConsumer : MessageConsumer<EventCollection>, IInitializer
    {
        #region Fields

        private readonly ConcurrentDictionary<Type, IEventHandler> _cachedHandlers;
        private readonly ICommandBus _commandBus;
        private readonly IMessageBus<IEvent> _eventBus;
        private readonly Dictionary<CompositeKey, Type> _eventTypesMapContractType;
        private readonly IEventPublishedVersionStore _publishedVersionStore;
        private readonly ISendReplyService _sendReplyService;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventConsumer"/> class.
        /// </summary>
        public EventConsumer(
            ISendReplyService sendReplyService, 
            ICommandBus commandBus, 
            IMessageBus<IEvent> eventBus, 
            IEventPublishedVersionStore publishedVersionStore, 
            ILoggerFactory loggerFactory, 
            IMessageReceiver<Envelope<EventCollection>> eventReceiver)
            : base(eventReceiver, loggerFactory.GetDefault(), "EventCollection")
        {
            this._eventTypesMapContractType = new Dictionary<CompositeKey, Type>();
            this._cachedHandlers = new ConcurrentDictionary<Type, IEventHandler>();

            this._publishedVersionStore = publishedVersionStore;
            this._sendReplyService = sendReplyService;
            this._commandBus = commandBus;
            this._eventBus = eventBus;

            // this._retryQueue = new BlockingCollection<Envelope<EventCollection>>();
        }

        #endregion

        #region Methods and Operators

        public override void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            IEnumerable<Type> eventHandlerInterfaceTypes =
                assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                    .Where(FilterType)
                    .SelectMany(type => type.GetInterfaces())
                    .Where(FilterInterfaceType);

            foreach (Type eventHandlerType in eventHandlerInterfaceTypes)
            {
                Type[] genericTypes = eventHandlerType.GetGenericArguments();
                if (genericTypes.Length == 1)
                {
                    this.RegisterEventHandler(container, eventHandlerType);
                    continue;
                }

                var key = new CompositeKey(genericTypes);

                if (this._eventTypesMapContractType.ContainsKey(key))
                {
                    string errorMessage =
                        string.Format(
                            "There are have duplicate IEventHandler interface type for '{0}'.", 
                            string.Join(",", key.Select(item => item.FullName)));
                    throw new SystemException(errorMessage);
                }

                this._eventTypesMapContractType[key] = eventHandlerType;
            }

            foreach (Type eventHandlerType in this._eventTypesMapContractType.Values)
            {
                this.RegisterEventHandler(container, eventHandlerType);
            }
        }

        protected override void OnMessageReceived(object sender, Envelope<EventCollection> envelope)
        {
            // var traceInfo = new TraceInfo(
            // Convert.ToString(envelope.Metadata["processId"]),
            // Convert.ToString(envelope.Metadata["replyAddress"]));

            // var sourceInfo = new SourceKey(
            // Convert.ToString(envelope.Metadata["sourceId"]),
            // Convert.ToString(envelope.Metadata["sourceNamespace"]),
            // Convert.ToString(envelope.Metadata["sourceTypeName"]),
            // Convert.ToString(envelope.Metadata["sourceAssemblyName"]));
            var sourceInfo = (SourceInfo)envelope.Items["SourceInfo"];
            var traceInfo = (TraceInfo)envelope.Items["TraceInfo"];
            int version = envelope.Body.Version;

            if (version > 1)
            {
                int lastPublishedVersion = this._publishedVersionStore.GetPublishedVersion(sourceInfo) + 1;
                if (lastPublishedVersion < version)
                {
                    var bus = sender as IMessageBus<EventCollection>;
                    if (bus != null) {
                        bus.Send(envelope);
                    }
                    // _retryQueue.Add(envelope);
                    if (this.logger.IsDebugEnabled)
                    {
                        this.logger.DebugFormat(
                            "The event cannot be process now as the version is not the next version, it will be handle later. aggregateRootType={0},aggregateRootId={1},lastPublishedVersion={2},eventVersion={3}", 
                            sourceInfo.GetSourceTypeName(), 
                            sourceInfo.Id, 
                            lastPublishedVersion, 
                            version);
                    }

                    return;
                }

                if (lastPublishedVersion > version)
                {
                    if (this.logger.IsDebugEnabled)
                    {
                        this.logger.DebugFormat(
                            "The event is ignored because it is obsoleted. aggregateRootType={0},aggregateRootId={1},lastPublishedVersion={2},eventVersion={3}", 
                            sourceInfo.GetSourceTypeName(), 
                            sourceInfo.Id, 
                            lastPublishedVersion, 
                            version);
                    }

                    return;
                }
            }

            var eventContext = new EventContext(this._commandBus, this._sendReplyService)
                                   {
                                       SourceInfo = sourceInfo, 
                                       TraceInfo = traceInfo, 
                                       Version = version
                                   };

            this.ProcessEvents(envelope.Body.Select(EventDescriptor.Create).ToArray(), eventContext);

            this._publishedVersionStore.AddOrUpdatePublishedVersion(sourceInfo, version);

            Envelope<IEvent>[] events =
                envelope.Body.Select(@event => BuildEnvelopedEvent(@event, sourceInfo.Id, envelope.CorrelationId))
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

        private static bool FilterInterfaceType(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IEventHandler<>) || genericType == typeof(IEventHandler<,>)
                   || genericType == typeof(IEventHandler<,,>) || genericType == typeof(IEventHandler<,,,>)
                   || genericType == typeof(IEventHandler<,,,,>);
        }

        private static bool FilterType(Type type)
        {
            if (type.IsInterface)
            {
                return false;
            }

            if (type.IsAbstract)
            {
                return false;
            }

            return type.GetInterfaces().Any(FilterInterfaceType);
        }

        private IEventHandler GetEventHandler(Type[] eventTypes, out Type eventHandlerType)
        {
            eventHandlerType = this.GetEventHandlerInterfaceType(eventTypes);

            IEventHandler cachedHandler;
            if (this._cachedHandlers.TryGetValue(eventHandlerType, out cachedHandler))
            {
                return cachedHandler;
            }

            throw new HandlerNotFoundException(eventTypes);
        }

        private Type GetEventHandlerInterfaceType(Type[] eventTypes)
        {
            switch (eventTypes.Length)
            {
                case 0:
                    throw new ArgumentNullException("eventTypes", "An empty array.");
                case 1:
                    return typeof(IEventHandler<>).MakeGenericType(eventTypes[0]);
                default:
                    return this._eventTypesMapContractType[new CompositeKey(eventTypes)];
            }
        }

        private object[] GetParameters(
            IEventContext eventContext, 
            IEnumerable<EventDescriptor> events, 
            IEnumerable<Type> parameterTypes)
        {
            var array = new ArrayList();
            array.Add(eventContext);

            foreach (Type parameterType in parameterTypes)
            {
                EventDescriptor descriptor = events.FirstOrDefault(p => p.EventType == parameterType);
                if (descriptor != null)
                {
                    array.Add(descriptor.Event);
                }
            }

            return array.ToArray();
        }

        private void ProcessEvents(EventDescriptor[] events, EventContext eventContext)
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

            if (parameters.Length - 1 != events.Length)
            {
                throw new SystemException();
            }

            // TryMultipleInvokeHandlerMethod(eventHandler, parameters);
            try
            {
                this.TryMultipleInvoke(
                    this.TryInvokeHandlerMethod, 
                    eventHandler, 
                    parameters, 
                    handler => eventHandler.GetType().FullName, 
                    messages => string.Join(", ", messages.Skip(1).Select(msg => msg.ToString())));
            }
            catch (Exception)
            {
            }
            finally
            {
                eventContext.Commit();
            }
        }

        private void RegisterEventHandler(IObjectContainer container, Type eventHandlerType)
        {
            string typeNames = string.Join(", ", eventHandlerType.GetGenericArguments().Select(item => item.FullName));
            List<IEventHandler> eventHandlers = container.ResolveAll(eventHandlerType).OfType<IEventHandler>().ToList();
            switch (eventHandlers.Count)
            {
                case 0:
                    throw new SystemException(
                        string.Format("The type('{0}') of event handler is not found.", typeNames));
                case 1:
                    this._cachedHandlers[eventHandlerType] = eventHandlers.First();
                    break;
                default:
                    throw new SystemException(
                        string.Format("Found more than one event handler for '{0}' with IEventHandler<>.", typeNames));
            }
        }

        private void TryInvokeHandlerMethod(IEventHandler eventHandler, object[] parameters)
        {
            switch (parameters.Length - 1)
            {
                case 1:
                    ((dynamic)eventHandler).Handle((dynamic)parameters[0], (dynamic)parameters[1]);
                    break;
                case 2:
                    ((dynamic)eventHandler).Handle(
                        (dynamic)parameters[0], 
                        (dynamic)parameters[1], 
                        (dynamic)parameters[2]);
                    break;
                case 3:
                    ((dynamic)eventHandler).Handle(
                        (dynamic)parameters[0], 
                        (dynamic)parameters[1], 
                        (dynamic)parameters[2], 
                        (dynamic)parameters[3]);
                    break;
                case 4:
                    ((dynamic)eventHandler).Handle(
                        (dynamic)parameters[0], 
                        (dynamic)parameters[1], 
                        (dynamic)parameters[2], 
                        (dynamic)parameters[3], 
                        (dynamic)parameters[4]);
                    break;
                case 5:
                    ((dynamic)eventHandler).Handle(
                        (dynamic)parameters[0], 
                        (dynamic)parameters[1], 
                        (dynamic)parameters[2], 
                        (dynamic)parameters[3], 
                        (dynamic)parameters[4], 
                        (dynamic)parameters[5]);
                    break;
            }
        }

        #endregion


        private struct CompositeKey : IEnumerable<Type>
        {
            #region Fields

            private readonly IEnumerable<Type> types;

            #endregion

            #region Constructors and Destructors

            public CompositeKey(IEnumerable<Type> types)
            {
                if (types.Distinct().Count() != types.Count())
                {
                    throw new ArgumentException("There are have duplicate types.", "types");
                }

                this.types = types;
            }

            #endregion

            #region Methods and Operators

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != this.GetType())
                {
                    return false;
                }

                var other = (CompositeKey)obj;

                return this.Except(other).IsEmpty();
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return this.types.GetEnumerator();
            }

            public override int GetHashCode()
            {
                return
                    this.types.OrderBy(type => type.FullName)
                        .Select(type => type.GetHashCode())
                        .Aggregate((x, y) => x ^ y);
            }

            #endregion

            #region Explicit Interface Methods

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (Type type in this.types)
                {
                    yield return type;
                }
            }

            #endregion
        }

        private class EventDescriptor
        {
            public IEvent Event { get; set; }

            public Type EventType { get; set; }


            public static EventDescriptor Create(IEvent @event)
            {
                return new EventDescriptor { Event = @event, EventType = @event.GetType() };
            }
        }
    }
}