using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using ThinkLib;


namespace ThinkNet.Database.NHibernate
{
    public class NHibernateContext : DataContextBase, INHibernateContext
    {
        private readonly ISession _session;
        public NHibernateContext(ISession session)
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
            entity.NotNull("entity");
            return Session.Contains(entity);
        }

        protected override void Delete(object entity, Func<object, bool> beforeDelete)
        {
            entity.NotNull("entity");

            if (beforeDelete(entity))
                Session.Delete(entity);
        }

        public override void Detach(object entity)
        {
            entity.NotNull("entity");
            Session.Evict(entity);
        }

        public override void Attach(object entity)
        {
            entity.NotNull("entity");
            Session.Merge(entity);
        }

        protected override void Save(object entity, Func<object, bool> beforeSave)
        {
            entity.NotNull("entity");

            if (beforeSave(entity))
                Session.Save(entity);
        }

        protected override void Update(object entity, Func<object, bool> beforeUpdate)
        {
            entity.NotNull("entity");

            if (beforeUpdate(entity))
                Session.Update(entity);
        }

        protected override void SaveOrUpdate(object entity, Func<object, bool> beforeSave, Func<object, bool> beforeUpdate)
        {
            entity.NotNull("entity");
            Session.SaveOrUpdate(entity);
        }

        public override void Refresh(object entity)
        {
            entity.NotNull("entity");
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

        public override void Load(object entity)
        {
            entity.NotNull("entity");

            Session.Load(entity, entity);
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
