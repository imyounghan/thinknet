using System;
using System.Data.Common;
using System.Data.Entity;
using ThinkLib.Contexts;

namespace ThinkNet.Database.EntityFramework
{
    public class EntityFrameworkContextFactory : ContextManager, IDataContextFactory
    {
        private readonly Func<DbContext> _contextFactory;
        private readonly Type _dbContextType;
        public EntityFrameworkContextFactory(Func<DbContext> contextFactory, string contextType = null)
            : this(contextFactory, null, contextType)
        { }

        public EntityFrameworkContextFactory(Type dbContextType, string contextType = null)
            : this(null, dbContextType, contextType)
        { }

        public EntityFrameworkContextFactory(Func<DbContext> contextFactory, Type dbContextType, string contextType = null)
            : base(contextType)
        {
            this._contextFactory = contextFactory;
            this._dbContextType = dbContextType;
        }


        public IDataContext GetCurrentDataContext()
        {
            return base.CurrentContext.CurrentContext() as IDataContext;
        }

        public IDataContext CreateDataContext()
        {
            DbContext dbContext;
            if (_contextFactory == null) {
                dbContext = (DbContext)Activator.CreateInstance(_dbContextType);
            }
            else {
                dbContext = _contextFactory.Invoke();
            }
            return new EntityFrameworkContext(dbContext, this);
        }


        public IDataContext CreateDataContext(string nameOrConnectionString)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(string) });
            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(string) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            var dbContext = (DbContext)constructor.Invoke(new object[] { nameOrConnectionString });

            return new EntityFrameworkContext(dbContext, this);
        }

        public IDataContext CreateDataContext(DbConnection connection)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(DbConnection) });
            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(DbConnection) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            var dbContext = (DbContext)constructor.Invoke(new object[] { connection });

            return new EntityFrameworkContext(dbContext, this);
        }
    }
}
