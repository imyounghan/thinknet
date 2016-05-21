using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkNet.Messaging;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Configurations;

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
            private readonly ILogger _logger;
            private readonly ConcurrentDictionary<Type, object> _repositories;

            public RepositoryContext(IDataContext context, IMemoryCache cache, IEventBus eventBus)
            {
                this._repositories = new ConcurrentDictionary<Type, object>();
                this._context = context;
                this._cache = cache;
                this._eventBus = eventBus;
                this._logger = LogManager.GetLogger("ThinkNet");
            }

            public void Commit()
            {
                _context.Commit();

                var events = _context.TrackingObjects.OfType<IEventPublisher>()
                    .SelectMany(item => item.Events).ToList();

                if (events.Count == 0) {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("commit all aggregateRoots. count:{0}.", _context.TrackingObjects.Count);
                    return;
                }

                _eventBus.Publish(events);

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("commit all aggregateRoots then publish all events. count:{0}, data:[{1}].",
                        _context.TrackingObjects.Count,
                        string.Join("|", events.Select(item => item.ToString())));
            }

            private object CreateRepository(Type repositoryType)
            {
                var serviceType = GetRepositoryType(repositoryType);
                var constructor = serviceType.GetConstructor(new[] { typeof(IDataContext), typeof(IMemoryCache) });
                if (constructor != null) {
                    return constructor.Invoke(new object[] { _context, _cache });
                }
                constructor = serviceType.GetConstructor(new[] { typeof(IDataContext) });
                if (constructor != null) {
                    return constructor.Invoke(new object[] { _context });
                }


                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(IDbContext) or .ctor(IDbContext,IMemoryCache)", serviceType.FullName);
                if (_logger.IsErrorEnabled)
                    _logger.Error(errorMessage);

                throw new ThinkNetException(errorMessage);
            }

            private Type GetRepositoryType(Type repositoryType)
            {
                Type implementationType = null;
                if (!repositoryMap.TryGetValue(repositoryType, out implementationType))
                {
                    if (!TypeHelper.IsRepositoryInterfaceType(repositoryType)) {
                        string errorMessage = string.Format("The type of '{0}' does not extend interface IRepository<>.", repositoryType.FullName);
                        if (_logger.IsErrorEnabled)
                            _logger.Error(errorMessage);
                        throw new ThinkNetException(errorMessage);
                    }

                    var aggregateRootType = repositoryType.GetGenericArguments().Single();
                    implementationType = typeof(Repository<>).MakeGenericType(aggregateRootType);
                    //repositoryMap[repositoryType] = implementationType;
                    repositoryMap.TryAdd(repositoryType, implementationType);
                }

                return implementationType;
            }

            public TRepository GetRepository<TRepository>() where TRepository : class, IRepository<IAggregateRoot>
            {
                return (TRepository)_repositories.GetOrAdd(typeof(TRepository), CreateRepository);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _repositories.Clear();
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
            return new RepositoryContext(_dbContextFactory.CreateDataContext(), _cache, _eventBus);
        }

        /// <summary>
        /// 获取当前的仓储上下文
        /// </summary>
        public IRepositoryContext GetCurrentRepositoryContext()
        {
            var dbContext = _dbContextFactory.GetCurrentDataContext();

            return new RepositoryContext(dbContext, _cache, _eventBus);
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
            if (type.IsNull() || type == typeof(Object))
                return false;

            var result = type.IsClass && !type.IsAbstract &&
                type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(Repository<>);

            if (!result)
            {
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
