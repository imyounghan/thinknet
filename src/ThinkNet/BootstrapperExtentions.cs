using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkNet.Contracts;
using ThinkNet.Database;
using ThinkNet.Domain;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Domain.Repositories;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime;
using ThinkNet.Runtime.Routing;
using ThinkNet.Runtime.Writing;

namespace ThinkNet
{
    public static class BootstrapperExtentions
    {
        private static bool IsRepositoryInterfaceType(Type type)
        {
            if (!type.IsGenericType)
                return false;
            
            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IRepository<>);
        }

        private static bool IsQueryFetcherInterfaceType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IQueryFetcher<,>) ||
                genericType == typeof(IQueryMultipleFetcher<,>) ||
                genericType == typeof(IQueryPageFetcher<,>);
        }

        private static bool IsMessageHandlerInterfaceType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        private static void RegisterHanders(IEnumerable<Type> types)
        {
            foreach (var type in types.Where(p => p.IsClass && !p.IsAbstract)) {
                var lifecycle = LifeCycleAttribute.GetLifecycle(type);

                var interfaceTypes = type.GetInterfaces();
                foreach (var interfaceType in interfaceTypes.Where(IsMessageHandlerInterfaceType)) {
                    //this.Register(interfaceType, type, type.FullName, lifecycle);
                }
            }
        }

        private void RegisterFrameworkComponents(Bootstrapper bootstrapper)
        {
            bootstrapper.SetDefault<IDataContextFactory, MemoryContextFactory>();
            bootstrapper.SetDefault<IEventStore, EventStore>();
            bootstrapper.SetDefault<ISnapshotPolicy, NoneSnapshotPolicy>();
            bootstrapper.SetDefault<ISnapshotStore, SnapshotStore>();
            bootstrapper.SetDefault<ICache, LocalCache>();
            bootstrapper.SetDefault<IRoutingKeyProvider, DefaultRoutingKeyProvider>();
            bootstrapper.SetDefault<IEventSourcedRepository, EventSourcedRepository>();
            bootstrapper.SetDefault<IRepository, Repository>();
            bootstrapper.SetDefault<IMessageBus, MessageBus>();
            bootstrapper.SetDefault<ICommandService, CommandService>();
            bootstrapper.SetDefault<ICommandResultNotification, CommandService>();
            bootstrapper.SetDefault<IMessageHandlerRecordStore, MessageHandlerRecordInMemory>();
            bootstrapper.SetDefault<IEnvelopeSender, EnvelopeHub>();
            bootstrapper.SetDefault<IEnvelopeReceiver, EnvelopeHub>();
            bootstrapper.SetDefault<IProcessor, Processor>("core");
        }

        public static void StartThinkNet()
        {
        }
    }
}
