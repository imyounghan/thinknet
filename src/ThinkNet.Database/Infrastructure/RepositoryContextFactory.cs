using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ThinkNet.Database;
using ThinkNet.Database.Context;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    [Register(typeof(IRepositoryContextFactory))]
    public class RepositoryContextFactory : ContextManager, IRepositoryContextFactory, IInitializer
    {
        class RepositoryContext : DisposableObject, IRepositoryContext, IContext
        {
            private readonly IDataContext _context;
            private readonly IEventBus _eventBus;
            private readonly ICache _cache;
            private readonly IContextManager _contextManager;
            private readonly ConcurrentDictionary<Type, object> _repositories;

            public RepositoryContext(IDataContext context, ICache cache, IEventBus eventBus)
                : this(context, cache, eventBus, null)
            { }

            public RepositoryContext(IDataContext context, ICache cache, IEventBus eventBus, IContextManager contextManager)
            {
                this._repositories = new ConcurrentDictionary<Type, object>();
                this._context = context;
                this._cache = cache;
                this._eventBus = eventBus;
                this._contextManager = contextManager;
            }

            public void Commit()
            {
                _context.Commit();

                var events = _context.TrackingObjects.OfType<IEventPublisher>()
                    .SelectMany(item => item.Events).ToList();

                if (events.Count == 0) {
                    if (LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("commit all aggregateRoots. count:{0}.", _context.TrackingObjects.Count);
                    return;
                }

                _eventBus.Publish(events);

                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("commit all aggregateRoots then publish all events. count:{0}, data:[{1}].",
                        _context.TrackingObjects.Count,
                        string.Join("|", events.Select(item => item.ToString())));
            }

            private object CreateRepository(Type aggregateRootType)
            {
                var repositoryType = repositoryMap.GetOrAdd(aggregateRootType, MakeDefaultRepositoryType);
                var constructor = repositoryType.GetConstructor(new[] { 
                    typeof(IDataContext), 
                    typeof(ICache)
                });
                if (constructor != null) {
                    return constructor.Invoke(new object[] { _context, _cache });
                }

                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(IDataContext, ICache, ILogger)", repositoryType.FullName);
                throw new ThinkNetException(errorMessage);
            }

            private Type MakeDefaultRepositoryType(Type aggregateRootType)
            {
                return typeof(Repository<>).MakeGenericType(aggregateRootType);
            }
            
            public IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
                where TAggregateRoot : class, IAggregateRoot
            {
                return (IRepository<TAggregateRoot>)_repositories.GetOrAdd(typeof(TAggregateRoot), CreateRepository);
            }

            public TRepository GetRepository<TRepository, TAggregateRoot>()
                where TRepository : class, IRepository<TAggregateRoot>
                where TAggregateRoot : class, IAggregateRoot
            {
                var aggregateRootType = typeof(TAggregateRoot);

                Type repositoryType;
                if (!repositoryMap.TryGetValue(aggregateRootType, out repositoryType) || 
                    repositoryType != typeof(TRepository)) {
                    string errorMessage = string.Format("Can't found the repository from type of '{0}'.", aggregateRootType.FullName);
                    throw new ThinkNetException(errorMessage);
                }

                return (TRepository)_repositories.GetOrAdd(typeof(TAggregateRoot), CreateRepository);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _repositories.Clear();
                    _context.Dispose();
                }
            }

            IContextManager IContext.ContextManager
            {
                get { return this._contextManager; }
            }
        }


        private readonly IDataContextFactory _dbContextFactory;
        private readonly IEventBus _eventBus;
        private readonly ICache _cache;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public RepositoryContextFactory(IDataContextFactory dbContextFactory, IEventBus eventBus, ICache cache)
            : base(ConfigurationManager.AppSettings["thinkcfg.repository_context_type"])
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
            return new RepositoryContext(_dbContextFactory.CreateDataContext(), _cache, _eventBus);
        }

        /// <summary>
        /// 获取当前的仓储上下文
        /// </summary>
        public IRepositoryContext GetCurrentRepositoryContext()
        {
            //    var dbContext = _dbContextFactory.GetCurrentDataContext();
            //    return new RepositoryContext(dbContext, _cache, _eventBus);

            return base.CurrentContext.GetContext() as IRepositoryContext;
        }


        internal static readonly ConcurrentDictionary<Type, Type> repositoryMap = new ConcurrentDictionary<Type, Type>();

        private void RegisterType(Type type)
        {
            type.GetInterfaces().Where(TypeHelper.IsRepositoryInterfaceType)
                .ForEach(repository =>
                {
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
        void IInitializer.Initialize(IEnumerable<Type> types)
        {
            types.Where(IsRepositoryType).ForEach(RegisterType);
        }
    }
}
