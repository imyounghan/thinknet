using System.Data.Common;

namespace ThinkNet.Database.NHibernate
{
    [Register(typeof(IDataContextFactory))]
    public class NHibernateContextFactory : IDataContextFactory
    {
        public IDataContext CreateDataContext()
        {
            var session = NHibernateSessionBuilder.Instance.OpenSession();
            return new NHibernateContext(session);
        }

        public IDataContext CreateDataContext(string nameOrConnectionString)
        {
            throw new System.NotImplementedException();
        }

        public IDataContext CreateDataContext(DbConnection connection)
        {
            var session = NHibernateSessionBuilder.Instance.OpenSession(connection);
            return new NHibernateContext(session);
        }
    }
}
