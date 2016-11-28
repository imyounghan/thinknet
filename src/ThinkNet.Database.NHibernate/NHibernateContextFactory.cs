using System.Data.Common;
using ThinkLib.Annotation;
using ThinkNet.Database.Context;

namespace ThinkNet.Database.NHibernate
{
    [Register(typeof(IDataContextFactory))]
    public class NHibernateContextFactory : ContextManager, IDataContextFactory
    {
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

        public IDataContext GetCurrent()
        {
            var session = NHibernateSessionBuilder.Instance.GetSession();
            return new NHibernateContext(session);
        }
    }
}
