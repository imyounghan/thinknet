using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace ThinkNet.Database
{
    public class MemoryContextFactory : IDataContextFactory
    {
        private static ConcurrentDictionary<Type, Hashtable> total;
        private static readonly object lockObj;

        static MemoryContextFactory()
        {
            total = new ConcurrentDictionary<Type, Hashtable>();
            lockObj = new object();
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
            return new MemoryContext();
        }

        public IDataContext CreateDataContext(DbConnection connection)
        {
            throw new NotImplementedException();
        }

        public enum EntityState
        {
            Detached = 1,
            Unchanged = 2,
            Added = 4,
            Deleted = 8,
            Modified = 16,
        }

        class MemoryContext : DataContextBase
        {
            private readonly Dictionary<string, KeyValuePair<object, EntityState>> local;
            public MemoryContext()
            {
                this.local = new Dictionary<string, KeyValuePair<object, EntityState>>();
            }


            public override IDbConnection DbConnection
            {
                get { throw new NotImplementedException(); }
            }

            protected override void Dispose(bool disposing)
            { }

            public override ICollection TrackingObjects
            {
                get { return local.Values.Select(p => p.Key).ToArray(); }
            }

            
            protected override void DoCommit()
            {
                foreach (var entry in local.Values) {
                    var entityType = entry.Key.GetType();
                    var entities = total.GetOrAdd(entityType, (arg) => Hashtable.Synchronized(new Hashtable()));
                    
                    switch (entry.Value) {
                        case EntityState.Added:
                        case EntityState.Modified:
                            entities[GetKey(entry.Key)] = entry.Key;
                            break;
                        case EntityState.Deleted:
                            entities.Remove(GetKey(entry.Key));
                            break;
                    }
                }
            }

            private static string GetKey(object entity)
            {
                return string.Concat(entity.GetType().FullName, "@", entity.GetHashCode());
            }


            public override bool Contains(object entity)
            {
                return local.ContainsKey(GetKey(entity));
            }

            public override void Attach(object entity)
            {
                local[GetKey(entity)] = new KeyValuePair<object, EntityState>(entity, EntityState.Unchanged);
            }

            public override void Detach(object entity)
            {
                local[GetKey(entity)] = new KeyValuePair<object, EntityState>(entity, EntityState.Detached);
            }

            public override object Find(Type type, object id)
            {
                try {
                    var entity  = Activator.CreateInstance(type, id);
                    this.Load(entity);
                    return entity;
                }
                catch (Exception) {
                    return null;
                } 
            }

            public override object Find(Type type, object[] keyValues)
            {                
                try {
                    var entity  = Activator.CreateInstance(type, keyValues);
                    this.Load(entity);
                    return entity;
                }
                catch (Exception) {
                    return null;
                }                
            }

            public override void Refresh(object entity)
            { }

            protected override void Save(object entity, Func<object, bool> beforeSave)
            {
                if (!beforeSave(entity))
                    return;

                var key = GetKey(entity);
                if (this.Contains(entity)) {
                    switch (local[key].Value) {
                        case EntityState.Deleted:
                            throw new Exception("The object cannot be registered as a new object since it was marked as deleted.");
                        case EntityState.Modified:
                            throw new Exception("The object cannot be registered as a new object since it was marked as modified.");
                    }
                }

                local[key] = new KeyValuePair<object, EntityState>(entity, EntityState.Added);
            }

            protected override void Update(object entity, Func<object, bool> beforeUpdate)
            {
                if (!beforeUpdate(entity))
                    return;

                var key = GetKey(entity);
                if (this.Contains(entity)) {
                    switch (local[key].Value) {
                        case EntityState.Deleted:
                            throw new Exception("The object cannot be registered as a new object since it was marked as deleted.");
                        case EntityState.Added:
                            throw new Exception("The object cannot be registered as a modified object since it was marked as created.");
                    }
                }

                local[key] = new KeyValuePair<object, EntityState>(entity, EntityState.Deleted);
            }

            protected override void Delete(object entity, Func<object, bool> beforeDelete)
            {
                if (!beforeDelete(entity))
                    return;
                
                var key =GetKey(entity);
                if (this.Contains(entity)) {

                    switch (local[key].Value) {
                        case EntityState.Detached:
                        case EntityState.Unchanged:
                        case EntityState.Added:
                            local.Remove(key);
                            break;
                    }
                }

                local[key] = new KeyValuePair<object, EntityState>(entity, EntityState.Deleted);
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

            public override void Load(object entity)
            {
                var key = GetKey(entity);
                if (this.Contains(entity)) {
                    entity = local[key].Key;
                    return;
                }

                Hashtable entities;
                if (total.TryGetValue(entity.GetType(), out entities)) {
                    entity = entities[key];
                }

                throw new EntityNotFoundException();
            }


            public override IQueryable<TEntity> CreateQuery<TEntity>()
            {
                Hashtable entities;
                if (total.TryGetValue(typeof(TEntity), out entities)) {
                    Hashtable clone;
                    lock (lockObj) {
                        clone = entities.Clone() as Hashtable;
                    }
                    return clone.Values.Cast<TEntity>().AsQueryable();
                    //return entities.Values.Cast<TEntity>().AsQueryable();
                    
                }

                return new EnumerableQuery<TEntity>(new TEntity[0]);
            }
        }        
    }
}
