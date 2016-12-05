using System.Data.Common;
using ThinkNet.Database.Context;

namespace ThinkNet.Database
{
    public class EntityFrameworkContextFactory : ContextManager, IDataContextFactory
    {
        private readonly IDbContextFactory _dbcontextFactory;
        public EntityFrameworkContextFactory(IDbContextFactory dbcontextFactory, string contextType)
            : base(contextType)
        {
            this._dbcontextFactory = dbcontextFactory;
        }

        public IDataContext Create()
        {
            var dbContext = _dbcontextFactory.Create();
            return new EntityFrameworkContext(dbContext);
        }

        public IDataContext Create(string nameOrConnectionString)
        {            
            var dbContext = _dbcontextFactory.Create(nameOrConnectionString);
            return new EntityFrameworkContext(dbContext);
        }

        public IDataContext Create(DbConnection connection)
        {
            var dbContext = _dbcontextFactory.Create(connection);
            return new EntityFrameworkContext(dbContext);
        }
        
        public IDataContext GetCurrent()
        {
            return CurrentContext.GetContext() as IDataContext;
        }
    }
}
