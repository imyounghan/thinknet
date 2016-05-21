using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkLib.Logging;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 仓储接口实现
    /// </summary>
    /// <typeparam name="TAggregateRoot">聚合类型</typeparam>
    public class Repository<TAggregateRoot> : IRepository<TAggregateRoot>
        where TAggregateRoot : class, IAggregateRoot
    {
        class EmptyCache : IMemoryCache
        {
            public static readonly IMemoryCache Instance = new EmptyCache();

            private EmptyCache()
            { }

            public object Get(System.Type type, object key)
            {
                return null;
            }

            public void Set(object entity, object key)
            { }

            public void Remove(System.Type type, object key)
            { }
        }

        private readonly IDataContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContext context)
            : this(context, EmptyCache.Instance)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContext context, IMemoryCache cache)
        {
            this._context = context;
            this._cache = cache;
            this._logger = LogManager.GetLogger("ThinkNet");
        }

        /// <summary>
        /// 数据上下文
        /// </summary>
        protected IDataContext DataContext
        {
            get { return this._context; }
        }
        /// <summary>
        /// 缓存程序
        /// </summary>
        protected IMemoryCache Cache
        {
            get { return this._cache; }
        }

        /// <summary>
        /// 添加聚合根到仓储
        /// </summary>
        public virtual void Add(TAggregateRoot aggregateRoot)
        {
            DataContext.Save(aggregateRoot);

        }
        void IRepository<TAggregateRoot>.Add(TAggregateRoot aggregateRoot)
        {
            this.Add(aggregateRoot);

            DataContext.DataCommitted += (sender, args) => {
                Cache.Set(aggregateRoot, aggregateRoot.Id);
            };

            if(_logger.IsDebugEnabled)
                _logger.DebugFormat("The aggregate root {0} of id {1} is added the dbcontext.",
                    typeof(TAggregateRoot).FullName, aggregateRoot.Id);
        }

        /// <summary>
        /// 从仓储中移除聚合
        /// </summary>
        public virtual void Remove(TAggregateRoot aggregateRoot)
        {
            DataContext.Delete(aggregateRoot);
        }
        void IRepository<TAggregateRoot>.Remove(TAggregateRoot aggregateRoot)
        {
            this.Remove(aggregateRoot);

            DataContext.DataCommitted += (sender, args) => {
                Cache.Remove(typeof(TAggregateRoot), aggregateRoot.Id);
            };

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("remove the aggregate root {0} of id {1} in dbcontext.",
                    typeof(TAggregateRoot).FullName, aggregateRoot.Id);
        }

        /// <summary>
        /// 根据标识id获取聚合实例，如未找到则返回null
        /// </summary>
        public virtual TAggregateRoot Find<TIdentify>(TIdentify id)
        {
            return DataContext.Find<TAggregateRoot>(id);
        }
        TAggregateRoot IRepository<TAggregateRoot>.Find<TIdentify>(TIdentify id)
        {
            var aggregateRoot = (TAggregateRoot)_cache.Get(typeof(TAggregateRoot), id);
            
            if (aggregateRoot == null) {
                aggregateRoot = this.Find(id);

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                        typeof(TAggregateRoot).FullName, id);

                Cache.Set(aggregateRoot, aggregateRoot.Id);
            }
            else {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        typeof(TAggregateRoot).FullName, id);


                DataContext.DataCommitted += (sender, args) => {
                    Cache.Set(aggregateRoot, aggregateRoot.Id);
                };
            }


            return aggregateRoot;
        }
    }
}
