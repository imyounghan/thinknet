using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ThinkNet.Database
{
    internal class MemoryContextFactory : IDataContextFactory
    {
        class MemoryContext : DataContextBase
        {
            private static Dictionary<Type, ISet<object>> entityCollection = new Dictionary<Type, ISet<object>>();

            public override IDbConnection DbConnection
            {
                get { throw new NotImplementedException(); }
            }

            protected override void Dispose(bool disposing)
            { }

            public override ICollection TrackingObjects
            {
                get { return localNewCollection.Union(localModifiedCollection).Union(localDeletedCollection).ToArray(); }
            }

            private readonly List<object> localNewCollection = new List<object>();
            private readonly List<object> localModifiedCollection = new List<object>();
            private readonly List<object> localDeletedCollection = new List<object>();
            protected override void DoCommit()
            {
                foreach (var newObj in localNewCollection)
                {
                    Add(newObj);
                }
                foreach (var modifiedObj in localModifiedCollection)
                {
                    Remove(modifiedObj);
                    Add(modifiedObj);
                }
                foreach (var delObj in localDeletedCollection)
                {
                    Remove(delObj);
                }

                localNewCollection.Clear();
                localModifiedCollection.Clear();
                localDeletedCollection.Clear();
            }

            private void Add(object entity)
            {
                var entityType = entity.GetType();

                ISet<object> entities;

                if (!entityCollection.TryGetValue(entityType, out entities))
                {
                    entities = new HashSet<object>();
                    entityCollection.Add(entityType, entities);
                }

                if (!entities.Contains(entity))
                {
                    entities.Add(entity);
                    entityCollection[entityType] = entities;
                }
            }
            private void Remove(object entity)
            {
                var entityType = entity.GetType();

                ISet<object> entities;

                if (entityCollection.TryGetValue(entityType, out entities))
                {

                    entities.Remove(entity);
                    //var index = entities.IndexOf(entity);

                    //if (index != -1)
                    //{
                    //    entities.RemoveAt(index);
                    //    entityCollection[entityType] = entities;
                    //}
                }
            }

            public override bool Contains(object entity)
            {
                return localNewCollection.Any(item => entity.Equals(item))
                     || localModifiedCollection.Any(item => entity.Equals(item))
                     || entityCollection[entity.GetType()].Any(item => entity.Equals(item));
            }

            public override void Attach(object entity)
            {
                this.Add(entity);
            }

            public override void Detach(object entity)
            {
                if (localDeletedCollection.Contains(entity))
                {
                    localDeletedCollection.Remove(entity);
                }

                if (localModifiedCollection.Contains(entity))
                {
                    localModifiedCollection.Remove(entity);
                }

                if (localNewCollection.Contains(entity))
                {
                    localNewCollection.Remove(entity);
                }
            }

            public override object Find(Type type, object id)
            {
                Func<object, bool> expression = entity =>
                {
                    if (id.GetType() == type)
                        return id.Equals(entity);

                    return Activator.CreateInstance(type, id).Equals(entity);
                };

                if (localNewCollection.Any(expression))
                {
                    return localNewCollection.FirstOrDefault(expression);
                }

                if (localModifiedCollection.Any(expression))
                {
                    return localNewCollection.FirstOrDefault(expression);
                }

                ISet<object> entities;
                if (entityCollection.TryGetValue(type, out entities))
                {
                    return entities.First(expression);
                }

                return null;
            }

            public override object Find(Type type, params object[] keyValues)
            {
                return this.Find(type, Activator.CreateInstance(type, keyValues));
            }

            public override void Refresh(object entity)
            { }

            protected override void Save(object entity, Func<object, bool> beforeSave)
            {
                if (!beforeSave(entity))
                    return;

                if (localDeletedCollection.Contains(entity))
                    throw new Exception("The object cannot be registered as a new object since it was marked as deleted.");
                if (localModifiedCollection.Contains(entity))
                    throw new Exception("The object cannot be registered as a new object since it was marked as modified.");

                //if (localNewCollection.Contains(entity))
                //    throw new AggregateException("The object cannot be registered as a new object, because the same id already exist.");
                if (localNewCollection.Contains(entity))
                    localNewCollection.Remove(entity);

                localNewCollection.Add(entity);
            }

            protected override void Update(object entity, Func<object, bool> beforeUpdate)
            {
                if (!beforeUpdate(entity))
                    return;

                if (localDeletedCollection.Contains(entity))
                    throw new Exception("The object cannot be registered as a modified object since it was marked as deleted.");
                if (localNewCollection.Contains(entity))
                    throw new Exception("The object cannot be registered as a modified object since it was marked as created.");

                if (localModifiedCollection.Contains(entity))
                    localModifiedCollection.Remove(entity);

                localModifiedCollection.Add(entity);
            }

            protected override void Delete(object entity, Func<object, bool> beforeDelete)
            {
                if (!beforeDelete(entity))
                    return;

                if (localNewCollection.Contains(entity))
                {
                    if (localNewCollection.Remove(entity))
                        return;
                }
                bool removedFromModified = localModifiedCollection.Remove(entity);
                if (!localDeletedCollection.Contains(entity))
                {
                    localDeletedCollection.Add(entity);
                }
            }

            protected override void SaveOrUpdate(object entity, Func<object, bool> beforeSave, Func<object, bool> beforeUpdate)
            {
                if (this.Contains(entity)) {
                    this.Save(entity, beforeSave);
                }
                else {
                    this.Update(entity, beforeUpdate);
                }
            }


            public override IQueryable<TEntity> CreateQuery<TEntity>()
            {
                ISet<object> entities;
                if (entityCollection.TryGetValue(typeof(TEntity), out entities))
                {
                    IEnumerable<TEntity> data = entities.Cast<TEntity>();

                    return new EnumerableQuery<TEntity>(data);
                }

                return new EnumerableQuery<TEntity>(new TEntity[0]);
            }
        }


        public IDataContext GetCurrentDataContext()
        {
            throw new NotImplementedException();
        }

        public IDataContext CreateDataContext()
        {
            return new MemoryContext();
        }

        public IDataContext CreateDataContext(string nameOrConnectionString)
        {
            throw new NotImplementedException();
        }

        public IDataContext CreateDataContext(System.Data.Common.DbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
