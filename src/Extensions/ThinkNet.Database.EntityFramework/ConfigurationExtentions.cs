using System;
using System.Data.Entity;
using ThinkNet.Storage;
using ThinkNet.Storage.EntityFramework;

namespace ThinkNet.Configurations
{
    public static class ConfigurationExtentions
    {

        public static Configuration RegisterDbContextFactory(this Configuration that, Func<DbContext> contextFactory, string contextType = null)
        {
            return that.RegisterInstance<IDataContextFactory>(new EntityFrameworkContextFactory(contextFactory, contextType));
        }

        public static Configuration RegisterDbContextFactory(this Configuration that, Type dbContextType, string contextType = null)
        {
            return that.RegisterInstance<IDataContextFactory>(new EntityFrameworkContextFactory(dbContextType, contextType));
        }

        public static Configuration RegisterDbContextFactory(this Configuration that, Func<DbContext> contextFactory, Type dbContextType, string contextType = null)
        {
            return that.RegisterInstance<IDataContextFactory>(new EntityFrameworkContextFactory(contextFactory, dbContextType, contextType));
        }
    }
}
