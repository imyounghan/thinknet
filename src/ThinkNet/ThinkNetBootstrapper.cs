using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkNet.Contracts;
using ThinkNet.Database;
using ThinkNet.Database.Storage;
using ThinkNet.Domain;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Domain.Repositories;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime;
using ThinkNet.Runtime.Routing;

namespace ThinkNet
{
    public sealed class ThinkNetBootstrapper : Bootstrapper
    {
        public static readonly new ThinkNetBootstrapper Current = new ThinkNetBootstrapper();

        private ThinkNetBootstrapper()
        { }

        private static bool IsRepositoryInterfaceType(Type genericType)
        {
            return genericType == typeof(IRepository<>);
        }

        private static bool IsQueryFetcherInterfaceType(Type genericType)
        {
            return genericType == typeof(IQueryFetcher<,>) ||
                genericType == typeof(IQueryMultipleFetcher<,>) ||
                genericType == typeof(IQueryPageFetcher<,>);
        }

        private static bool IsMessageHandlerInterfaceType(Type genericType)
        {
            return genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>);
        }

        private static bool FilterType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return IsMessageHandlerInterfaceType(genericType) || IsQueryFetcherInterfaceType(genericType);
        }

        protected override void OnAssembliesLoaded(IEnumerable<Assembly> assemblies, IEnumerable<Type> nonAbstractTypes)
        {
            foreach (var type in nonAbstractTypes) {
                var lifecycle = LifeCycleAttribute.GetLifecycle(type);

                var interfaceTypes = type.GetInterfaces();
                foreach (var interfaceType in interfaceTypes.Where(FilterType)) {
                    this.SetDefault(interfaceType, type, type.FullName, lifecycle);
                }
            }

            this.RegisterFrameworkComponents();
        }

        public override void Start()
        {
            if (this.Status != ServerStatus.Started)
                ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Start());

            base.Start();
        }

        public override void Stop()
        {
            if (this.Status != ServerStatus.Stopped)
                ObjectContainer.Instance.ResolveAll<IProcessor>().ForEach(p => p.Stop());

            base.Stop();
        }

        private void RegisterFrameworkComponents()
        {
            this.SetDefault<IDataContextFactory, MemoryContextFactory>();
            this.SetDefault<IEventStore, EventStore>();
            this.SetDefault<ISnapshotPolicy, NoneSnapshotPolicy>();
            this.SetDefault<ISnapshotStore, SnapshotStore>();
            this.SetDefault<ICache, LocalCache>();
            this.SetDefault<IRoutingKeyProvider, DefaultRoutingKeyProvider>();
            this.SetDefault<IEventSourcedRepository, EventSourcedRepository>();
            this.SetDefault<IRepository, Repository>();
            this.SetDefault<IMessageBus, MessageBus>();
            this.SetDefault<ICommandService, CommandService>();
            this.SetDefault<ICommandResultNotification, CommandService>();
            this.SetDefault<IMessageHandlerRecordStore, MessageHandlerRecordInMemory>();
            this.SetDefault<IEnvelopeSender, EnvelopeHub>();
            this.SetDefault<IEnvelopeReceiver, EnvelopeHub>();
            this.SetDefault<IProcessor, Processor>("core");
        }
    }
}
