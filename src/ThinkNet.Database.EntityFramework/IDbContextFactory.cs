using System;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;

namespace ThinkNet.Database
{
    public interface IDbContextFactory
    {
        DbContext Create();

        DbContext Create(string nameOrConnectionString);

        DbContext Create(DbConnection connection);
    }

    internal class DefaultContextFactory : IDbContextFactory
    {
        private readonly Type _dbContextType;
        public DefaultContextFactory()
        {
            var typeName = ConfigurationManager.AppSettings["thinkcfg.ef_dbtypename"];
            if(string.IsNullOrWhiteSpace(typeName)) {
                throw new ThinkNetException("Config by 'thinkcfg.ef_dbtypename' is empty or not exist.");
            }

            var dbContextType = Type.GetType(typeName);
            if(dbContextType.IsAssignableFrom(typeof(DbContext)))
                throw new InvalidCastException();

            this._dbContextType = dbContextType;
        }

        public DbContext Create()
        {
            return (DbContext)Activator.CreateInstance(_dbContextType);
        }

        public DbContext Create(DbConnection connection)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(DbConnection) });
            if(constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(DbConnection) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            return (DbContext)constructor.Invoke(new object[] { connection });
        }

        public DbContext Create(string nameOrConnectionString)
        {
            var constructor = _dbContextType.GetConstructor(new[] { typeof(string) });
            if(constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor(string) ", _dbContextType.FullName);
                throw new InvalidCastException(errorMessage);
            }

            return (DbContext)constructor.Invoke(new object[] { nameOrConnectionString });
        }
    }
}
