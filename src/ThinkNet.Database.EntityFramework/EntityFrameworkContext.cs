using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using ThinkLib;

namespace ThinkNet.Database.EntityFramework
{
    public class EntityFrameworkContext : DataContextBase, IEntityFrameworkContext
    {
        public EntityFrameworkContext(DbContext efContext)
        {
            this._efContext = efContext;
        }

        public override IDbConnection DbConnection
        {
            get { return _efContext.Database.Connection; }
        }

        private readonly DbContext _efContext = null;
        public DbContext DbContext
        {
            get { return _efContext; }
        }

        public override ICollection TrackingObjects
        {
            get { return _efContext.ChangeTracker.Entries().Select(item => item.Entity).ToArray(); }
        }

        public override bool Contains(object entity)
        {
            entity.NotNull("entity");
            return _efContext.Entry(entity).State != EntityState.Detached;
        }

        public override void Detach(object entity)
        {
            entity.NotNull("entity");
            _efContext.Entry(entity).State = EntityState.Detached;
        }

        public override void Attach(object entity)
        {
            entity.NotNull("entity");
            _efContext.Entry(entity).State = EntityState.Unchanged;
        }

        protected override void Delete(object entity, Func<object, bool> beforeDelete)
        {
            entity.NotNull("entity");

            if (beforeDelete(entity))
                _efContext.Entry(entity).State = EntityState.Deleted;
        }

        protected override void Save(object entity, Func<object, bool> beforeSave)
        {
            entity.NotNull("entity");

            if (beforeSave(entity))
                _efContext.Entry(entity).State = EntityState.Added;
        }

        protected override void Update(object entity, Func<object, bool> beforeUpdate)
        {
            entity.NotNull("entity");

            if (beforeUpdate(entity))
                _efContext.Entry(entity).State = EntityState.Modified;
        }

        public override void Refresh(object entity)
        {
            entity.NotNull( "entity");
            _efContext.Entry(entity).Reload();
        }

        public override object Find(Type type, object id)
        {
            return _efContext.Set(type).Find(id);
        }

        public override object Find(Type type, params object[] keyValues)
        {
            return _efContext.Set(type).Find(keyValues);
        }

        public override void Load(object entity)
        {
            this.Refresh(entity);
        }

        protected override void DoCommit()
        {
            try {
                if (_efContext.ChangeTracker.HasChanges())
                    _efContext.SaveChanges();
            }
            catch {
                _efContext.ChangeTracker.DetectChanges();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _efContext != null) {
                try {
                    _efContext.Dispose();
                }
                catch (Exception) {
                    throw;
                }
            }
        }


        private MemberExpression GetMemberInfo(LambdaExpression lambda)
        {
            if (lambda == null)
                throw new ArgumentNullException("lambda");

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert) {
                memberExpr = ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess) {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("lambda");

            return memberExpr;
        }
        private string GetEagerLoadingPath<TEntity>(Expression<Func<TEntity, dynamic>> eagerLoadingProperty)
        {
            MemberExpression memberExpression = this.GetMemberInfo(eagerLoadingProperty);
            var parameterName = eagerLoadingProperty.Parameters.First().Name;
            var memberExpressionStr = memberExpression.ToString();
            var path = memberExpressionStr.Replace(parameterName + ".", string.Empty);
            return path;
        }

        public IQueryable<TEntity> CreateQuery<TEntity>(params Expression<Func<TEntity, dynamic>>[] eagerLoadingProperties) where TEntity : class
        {
            IQueryable<TEntity> query = DbContext.Set<TEntity>();
            foreach (var eagerLoadingProperty in eagerLoadingProperties) {
                var eagerLoadingPath = this.GetEagerLoadingPath(eagerLoadingProperty);
                query = query.Include(eagerLoadingPath);
            }

            return query;
        }

        public override IQueryable<TEntity> CreateQuery<TEntity>()
        {
            return DbContext.Set<TEntity>();
        }

        public IEnumerable<TEntity> SqlQuery<TEntity>(string sql, params object[] parameters)
            where TEntity : class
        {
            return DbContext.Database.SqlQuery<TEntity>(sql, parameters).ToList();
        }

        public int SqlExecute(string sql, params object[] parameters)
        {
            return DbContext.Database.ExecuteSqlCommand(sql, parameters);
        }
    }
}
