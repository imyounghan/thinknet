﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime.Executing;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Domain.EventSourcing
{
    public class EventStreamInnerHandler : IProxyHandler, IHandler
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly IMessageBus _bus;
        private readonly IEnvelopeSender _sender;

        private readonly Dictionary<CompositeKey, Type> _eventTypesMapContractType;

        public Type ContractType { get { return typeof(IHandler); } }

        public Type TargetType { get { return typeof(EventStreamInnerHandler); } }

        public EventStreamInnerHandler(IEnvelopeSender sender,
            IMessageBus messageBus, 
            IHandlerRecordStore handlerStore)
        {
            this._sender = sender;
            this._bus = messageBus;
            this._handlerStore = handlerStore;
            this._eventTypesMapContractType = new Dictionary<CompositeKey, Type>();
        }

        private Envelope Transform(EventStream @event, Exception ex)
        {
            var reply = @event.Events.IsEmpty() ? 
                new CommandResultReplied(@event.CorrelationId, CommandReturnType.DomainEventHandled, CommandStatus.NothingChanged) :
                new CommandResultReplied(@event.CorrelationId, CommandReturnType.DomainEventHandled, ex);

            var envelope = new Envelope(reply);
            envelope.Metadata[StandardMetadata.SourceId] = @event.CorrelationId;
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.MessageKind;

            return envelope;
        }


        public virtual void Handle(EventStream @event)
        {
            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            var handler = this.GetEventHandler(eventTypes);
            
            try {
                handler.Handle(this.GetParameter(@event));
                _sender.SendAsync(Transform(@event, null));
            }
            catch (DomainEventObsoletedException) {
                _sender.SendAsync(Transform(@event, null));
                _bus.Publish(@event.Events);
            }
            catch (DomainEventAsPendingException) {
                _bus.Publish(@event);
            }
            catch (Exception ex) {
                _sender.SendAsync(Transform(@event, ex));
                throw ex;
            }            
        }

        protected IProxyHandler GetEventHandler(IEnumerable<Type> types)
        {
            var handlerType = _eventTypesMapContractType[new CompositeKey(types)];
            var handlers = ObjectContainer.Instance.ResolveAll(handlerType)
                .Cast<IHandler>()
                .Select(handler => new EventHandlerWrapper(handler, handlerType))
                .ToArray();

            switch (handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(types);
                case 1:
                    return handlers[0];
                default:
                    throw new MessageHandlerTooManyException(types);
            }
        }
        
        protected object[] GetParameter(EventStream @event)
        {
            var array = new ArrayList();
            array.Add(new VersionData() {
                Key = new DataKey(@event.SourceId, @event.SourceType),
                Version = @event.Version
            });

            var collection = @event.Events as ICollection;
            if (collection != null) {
                array.AddRange(collection);
            }
            else {
                foreach (var el in @event.Events)
                    array.Add(el);
            }

            return array.ToArray();
        }

        #region IInitializer 成员
        private static bool IsEventHandlerInterface(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        public void Initialize(IEnumerable<Type> types)
        {
            foreach (var type in types.Where(p => p.IsClass && !p.IsAbstract)) {
                foreach (var interfaceType in type.GetInterfaces().Where(IsEventHandlerInterface)) {
                    var key = new CompositeKey(interfaceType.GenericTypeArguments);
                    _eventTypesMapContractType.TryAdd(key, interfaceType);
                }
            }
        }

        public void Handle(params object[] args)
        {
            this.Handle(args[0] as EventStream);
        }

        public IHandler GetTargetHandler()
        {
            return this;
        }

        #endregion

        class CompositeKey : IEnumerable<Type>
        {
            private readonly IEnumerable<Type> eventTypes;

            public CompositeKey(IEnumerable<Type> types)
            {
                if (types.Distinct().Count() != types.Count()) {
                    throw new ArgumentException("There are have duplicate types.", "types");
                }

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

                if (other == null)
                    return false;

                return this.Except(other).IsEmpty();
            }

            public override int GetHashCode()
            {
                return eventTypes.OrderBy(type => type.FullName).Select(type => type.GetHashCode()).Aggregate((x, y) => x ^ y);
            }
        }
    }
}
