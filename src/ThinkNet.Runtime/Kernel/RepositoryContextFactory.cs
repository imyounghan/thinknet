using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Annotation;
using ThinkNet.Component;
using ThinkNet.Core;
using ThinkNet.Kernel;
using ThinkNet.Messaging;
using ThinkNet.Runtime;
using ThinkNet.Runtime.Logging;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="IRepositoryContextFactory"/> 的系统默认实现
    /// </summary>
    [RegisterComponent(typeof(IRepositoryContextFactory))]
    public class RepositoryContextFactory : IRepositoryContextFactory, IInitializer
    {
        class RepositoryContext : DisposableObject, IRepositoryContext
        {
            private readonly IDataContext _context;
            private readonly IEventBus _eventBus;
            private readonly IMemoryCache _cache;
            private readonly ConcurrentDictionary<Type, object> _repositories;
            private readonly bool _ownsDataContext;

            public RepositoryContext(IDataContext context, IMemoryCache cache, IEventBus eventBus, bool ownsDataContext)
            {
                this._repositories = new ConcurrentDictionary<Type, object>();
                this._context = context;
                this._cache = cache;
                this._eventBus = eventBus;
                this._ownsDataContext = ownsDataContext;
            }

            public void Commit(string correlationId)
            {
                _context.Commit();

                var events = _context.TrackingObjects.OfType<IEventPublisher>()
                    .SelectMany(item => item.Events).ToList();

                if (events.Count == 0)
                    return;

                if (string.IsNullOrWhiteSpace(correlationId)) {
                    _eventBus.Publish(events);
                }
                else {
                    _eventBus.Publish(new DomainEventStream {
                        CommandId = correlationId,
                        Events = events
                    });
                }
                LogManager.GetLogger().Write(LoggingLevel.INFO, "publish all events. event ids: [{0}]", 
                    string.Join(",", events.Select(@event => @event.Id).ToArray()));
            }

            private object CreateRepository(Type repositoryType)
            {
                var serviceType = repositoryMap.GetOrAdd(repositoryType, MakeRepositoryType);
                var constructor = serviceType.GetConstructor(new[] { typeof(IDataContext), typeof(IMemoryCache) });
                if (constructor != null) {
                    return constructor.Invoke(new object[] { _context, _cache });
                }
                constructor = serviceType.GetConstructor(new[] { typeof(IDataContext) });
                if (constructor != null) {
                    return constructor.Invoke(new object[] { _context });
                }

                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(IDbContext) or .ctor(IDbContext,IMemoryCache)", serviceType.FullName);
                LogManager.GetLogger().Write(LoggingLevel.ERROR, errorMessage);
                throw new InvalidCastException(errorMessage);
            }

            private Type MakeRepositoryType(Type repositoryType)
            {
                if (!TypeHelper.IsRepositoryInterfaceType(repositoryType)) {
                    string errorMessage = string.Format("The repository type '{0}' does not extend interface IRepository<>.", repositoryType.FullName);
                    LogManager.GetLogger().Write(LoggingLevel.ERROR, errorMessage);
                    throw new SystemException(errorMessage);
                }

                var aggregateRootType = repositoryType.GetGenericArguments().Single();
                return typeof(Repository<>).MakeGenericType(aggregateRootType);
            }

            public TRepository GetRepository<TRepository>() where TRepository : class, IRepository
            {
                return (TRepository)_repositories.GetOrAdd(typeof(TRepository), CreateRepository);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) {
                    _repositories.Clear();
                    if (_ownsDataContext)
                        _context.Dispose();
                }
            }
        }


        private readonly IDataContextFactory _dbContextFactory;
        private readonly IEventBus _eventBus;
        private readonly IMemoryCache _cache;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RepositoryContextFactory(IDataContextFactory dbContextFactory, IEventBus eventBus, IMemoryCache cache)
        {
            this._dbContextFactory = dbContextFactory;
            this._eventBus = eventBus;
            this._cache = cache;
        }

        /// <summary>
        /// 创建一个仓储上下文实例。
        /// </summary>
        public IRepositoryContext CreateRepositoryContext()
        {
            var dbContext = _dbContextFactory.CreateDataContext();
            return new RepositoryContext(dbContext, _cache, _eventBus, true);
        }

        /// <summary>
        /// 获取当前的仓储上下文
        /// </summary>
        public IRepositoryContext GetCurrentRepositoryContext()
        {
            var dbContext = _dbContextFactory.GetCurrentDataContext();
            return new RepositoryContext(dbContext, _cache, _eventBus, false);
        }


        internal static readonly ConcurrentDictionary<Type, Type> repositoryMap = new ConcurrentDictionary<Type, Type>();

        private void RegisterType(Type type)
        {
            type.GetInterfaces().Where(TypeHelper.IsRepositoryInterfaceType)
                .ForEach(repository => {
                    repositoryMap.TryAdd(repository, type);
                });
        }

        private static bool IsRepositoryType(Type type)
        {
            if (type == null || type == typeof(Object))
                return false;

            var result = type.IsClass && !type.IsAbstract &&
                type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(Repository<>);

            if (!result) {
                return IsRepositoryType(type.BaseType);
            }

            return true;
        }
        void IInitializer.Initialize(IContainer container, IEnumerable<Type> types)
        {
            //var types = assemblies.SelectMany(assembly => assembly.GetTypes());
            types.Where(IsRepositoryType).ForEach(RegisterType);
        }
    }
}
