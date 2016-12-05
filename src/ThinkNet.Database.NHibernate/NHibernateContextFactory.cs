using System.Data.Common;
using ThinkNet.Database.Context;

namespace ThinkNet.Database
{
    public class NHibernateContextFactory : ContextManager, IDataContextFactory
    {
        public NHibernateContextFactory(string contextType)
            : base(contextType)
        { }

        public IDataContext Create()
        {
            var session = NHibernateSessionBuilder.Instance.OpenSession();
            return new NHibernateContext(session);
        }

        public IDataContext Create(string nameOrConnectionString)
        {
            throw new System.NotImplementedException();
        }

        public IDataContext Create(DbConnection connection)
        {
            var session = NHibernateSessionBuilder.Instance.OpenSession(connection);
            return new NHibernateContext(session);
        }

        public virtual IDataContext GetCurrent()
        {
            //var session = NHibernateSessionBuilder.Instance.GetSession();
            //return new NHibernateContext(session);
            return CurrentContext.GetContext() as IDataContext;
        }
    }
}
