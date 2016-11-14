using System;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using ThinkNet.Common;
using ThinkNet.Common.Context;

namespace ThinkNet.Database.EntityFramework
{
    [Register(typeof(IDataContextFactory))]
    public class EntityFrameworkContextFactory : ContextManager, IDataContextFactory
    {
        //private readonly Func<DbContext> _contextFactory;
        private readonly Type _dbContextType;
        //public EntityFrameworkContextFactory(Func<DbContext> contextFactory)
        //    : this(contextFactory, null)
        //{ }
                //public EntityFrameworkContextFactory(Type dbContextType)
        //    : this(null, dbContextType)
        //{ }
        //public EntityFrameworkContextFactory(Func<DbContext> contextFactory, Type dbContextType)
        //{
        //    this._contextFactory = contextFactory;
        //    this._dbContextType = dbContextType;
        //}

        public EntityFrameworkContextFactory()
        {
            var typeName = ConfigurationManager.AppSettings["thinkcfg.ef_dbtype"];
            if (string.IsNullOrWhiteSpace(typeName)) {
            }

            var dbContextType = Type.GetType(typeName);
            if (dbContextType.IsAssignableFrom(typeof(DbContext)))
                throw new InvalidCastException();

            this._dbContextType = dbContextType;
        }

        public IDataContext Create()
        {
            var dbContext = (DbContext)Activator.CreateInstance(_dbContextType);
            return new EntityFrameworkContext(dbContext);
        }


        public IDataContext Create(string nameOrConnectionString)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(string) });
            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(string) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            var dbContext = (DbContext)constructor.Invoke(new object[] { nameOrConnectionString });
            return new EntityFrameworkContext(dbContext);
        }

        public IDataContext Create(DbConnection connection)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(DbConnection) });
            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(DbConnection) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            var dbContext = (DbContext)constructor.Invoke(new object[] { connection });
            return new EntityFrameworkContext(dbContext);
        }
        
        public IDataContext GetCurrent()
        {
            return CurrentContext.GetContext() as IDataContext;
        }
    }
}
