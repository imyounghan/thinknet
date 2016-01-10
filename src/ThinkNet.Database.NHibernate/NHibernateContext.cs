using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using System.Data.Common;
using ThinkLib.Context;


namespace ThinkNet.Database.NHibernate
{
    public class NHibernateContext : DataContextBase, INHibernateContext
    {
        private readonly ISession _session;
        public NHibernateContext(ISession session)
            : this(session, null)
        { }

        internal NHibernateContext(ISession session, IContextManager contextManager)
            : base(contextManager)
        {
            this._session = session;            
        }
        

        /// <summary>
        /// 返回NHibernate的一个session
        /// </summary>
        public ISession Session
        {
            get
            {
                //if (_session == null) {
                //    try {
                //        if (_dbConnection != null && _dbConnection.State == ConnectionState.Open)
                //            _session = NHibernateFactory.Instance.OpenSession(_dbConnection);
                //        else
                //            _session = NHibernateFactory.Instance.OpenSession();

                //        _session.BeginTransaction();
                //    }
                //    catch { throw; }
                //}
                if (_session.Transaction == null)
                {
                    _session.BeginTransaction();
                }

                return _session;
            }
        }

        public override ICollection TrackingObjects
        {
            get { return Session.Statistics.EntityKeys.Select(item => Session.Get(item.EntityName, item.Identifier)).ToArray(); }
        }

        public override IDbConnection DbConnection
        {
            get
            {
                return Session.Connection;
            }
        }

        public override bool Contains(object entity)
        {
            return Session.Contains(entity);
        }

        public override void Delete(object entity)
        {
            Session.Delete(entity);
        }

        public override void Update(object entity)
        {
            Session.Update(entity);
        }

        public override void Detach(object entity)
        {
            Session.Evict(entity);
        }

        public override void Attach(object entity)
        {
            Session.Merge(entity);
        }

        public override void Save(object entity)
        {
            Session.Save(entity);
        }

        public override void Refresh(object entity)
        {
            Session.Refresh(entity);
        }

        public override object Find(Type type, object id)
        {
            return Session.Get(type, id);
        }

        public override object Find(Type type, params object[] keyValues)
        {
            if (keyValues.Length == 0)
                return Session.Get(type, keyValues[0]);

            var id = Activator.CreateInstance(type, keyValues);
            return Session.Get(type, id);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _session != null) {
                if (_session.IsOpen) {
                    _session.Close();
                }

                _session.Dispose();
            }
        }

        protected override void DoCommit()
        {
            if (_session == null)
                return;

            using (ITransaction transaction = Session.Transaction) {
                try {
                    transaction.Commit();
                }
                catch {
                    if (transaction.IsActive)
                        transaction.Rollback();
                    _session.Clear();
                    throw;
                }
            }
        }

        public override IQueryable<TEntity> CreateQuery<TEntity>()
        {
            return Session.Query<TEntity>();
        }

        public int SqlExecute(string sql, params object[] parameters)
        {
            var query = Session.CreateSQLQuery(sql);
            for (int index = 0; index < parameters.Length; index++) {
                query.SetParameter(index, parameters[index]);
            }
            return query.ExecuteUpdate();
        }

        public IEnumerable<TEntity> SqlQuery<TEntity>(string sql, params object[] parameters) where TEntity : class
        {
            var query = Session.CreateSQLQuery(sql);
            for (int index = 0; index < parameters.Length; index++) {
                query.SetParameter(index, parameters[index]);
            }
            return query.List<TEntity>();
        }
    }
}
