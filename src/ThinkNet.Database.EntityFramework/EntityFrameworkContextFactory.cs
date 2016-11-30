using System.Data.Common;
using ThinkLib.Annotation;
using ThinkNet.Database.Context;

namespace ThinkNet.Database.EntityFramework
{
    [Register(typeof(IDataContextFactory))]
    public class EntityFrameworkContextFactory : ContextManager, IDataContextFactory
    {
        private readonly IDbContextFactory _contextFactory;
        public EntityFrameworkContextFactory(IDbContextFactory contextFactory)
            : base("thread")
        {
            this._contextFactory = contextFactory;
        }

        public IDataContext Create()
        {
            var dbContext = _contextFactory.Create();
            return new EntityFrameworkContext(dbContext);
        }

        public IDataContext Create(string nameOrConnectionString)
        {            
            var dbContext = _contextFactory.Create(nameOrConnectionString);
            return new EntityFrameworkContext(dbContext);
        }

        public IDataContext Create(DbConnection connection)
        {
            var dbContext = _contextFactory.Create(connection);
            return new EntityFrameworkContext(dbContext);
        }
        
        public IDataContext GetCurrent()
        {
            return CurrentContext.GetContext() as IDataContext;
        }
    }
}
