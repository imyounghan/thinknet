using ThinkNet.Database;

namespace ThinkNet.Domain.Repositories
{
    /// <summary>
    /// 仓储接口实现
    /// </summary>
    /// <typeparam name="TAggregateRoot">聚合类型</typeparam>
    public class Repository<TAggregateRoot> : IRepository<TAggregateRoot>
        where TAggregateRoot : class, IAggregateRoot
    {
        private readonly IDataContextFactory _contextFactory;
        private readonly ICache _cache;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContextFactory contextFactory, ICache cache)
        {
            this._contextFactory = contextFactory;
            this._cache = cache;
        }

        /// <summary>
        /// 数据上下文
        /// </summary>
        protected IDataContext DataContext
        {
            get { return this._contextFactory.GetCurrent(); }
        }
        
        /// <summary>
        /// 添加聚合根到仓储
        /// </summary>
        public virtual void Add(TAggregateRoot aggregateRoot)
        {
            this.DataContext.Save(aggregateRoot);

        }
        void IRepository<TAggregateRoot>.Add(TAggregateRoot aggregateRoot)
        {
            this.Add(aggregateRoot);

            this.DataContext.DataCommitted += (sender, args) => {
                _cache.Set(aggregateRoot, aggregateRoot.Id);
            };

            if(LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is added the dbcontext.",
                    typeof(TAggregateRoot).FullName, aggregateRoot.Id);
        }

        /// <summary>
        /// 从仓储中移除聚合
        /// </summary>
        public virtual void Remove(TAggregateRoot aggregateRoot)
        {
            this.DataContext.Delete(aggregateRoot);
        }
        void IRepository<TAggregateRoot>.Remove(TAggregateRoot aggregateRoot)
        {
            this.Remove(aggregateRoot);

            this.DataContext.DataCommitted += (sender, args) => {
                _cache.Remove(typeof(TAggregateRoot), aggregateRoot.Id);
            };

            if (LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("remove the aggregate root {0} of id {1} in dbcontext.",
                    typeof(TAggregateRoot).FullName, aggregateRoot.Id);
        }

        /// <summary>
        /// 根据标识id获取聚合实例，如未找到则返回null
        /// </summary>
        public virtual TAggregateRoot Find<TIdentify>(TIdentify id)
        {
            return this.DataContext.Find<TAggregateRoot>(id);
        }
        TAggregateRoot IRepository<TAggregateRoot>.Find<TIdentify>(TIdentify id)
        {
            TAggregateRoot aggregateRoot = null;
            if (_cache.TryGet(typeof(TAggregateRoot), id, out aggregateRoot)) {
                this.DataContext.Attach(aggregateRoot);
                return aggregateRoot;
            }
            
            if (aggregateRoot == null) {
                aggregateRoot = this.Find(id);

                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                        typeof(TAggregateRoot).FullName, id);

                _cache.Set(aggregateRoot, aggregateRoot.Id);
            }
            else {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        typeof(TAggregateRoot).FullName, id);


                this.DataContext.DataCommitted += (sender, args) => {
                    _cache.Set(aggregateRoot, aggregateRoot.Id);
                };
            }


            return aggregateRoot;
        }
    }
}
