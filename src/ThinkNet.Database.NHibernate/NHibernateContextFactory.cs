using System.Data.Common;
using ThinkLib.Context;

namespace ThinkNet.Database.NHibernate
{
    public class NHibernateContextFactory : ContextManager, IDataContextFactory
    {
        public NHibernateContextFactory(string contextType)
            : base(contextType)
        { }

        public IDataContext CreateDataContext()
        {
            var session = NHibernateFactory.Instance.OpenSession();
            return new NHibernateContext(session, this);
        }

        public IDataContext GetCurrentDataContext()
        {
            return base.CurrentContext.CurrentContext() as IDataContext;
        }

        public IDataContext CreateDataContext(string nameOrConnectionString)
        {
            throw new System.NotImplementedException();
        }

        public IDataContext CreateDataContext(DbConnection connection)
        {
            var session = NHibernateFactory.Instance.OpenSession(connection);
            return new NHibernateContext(session, this);
        }
    }
}
