using System;
using System.Collections;
using System.Data;
using System.Linq;

using ThinkNet.Infrastructure;
using ThinkLib.Common;


namespace ThinkNet.Database
{
    /// <summary>
    /// 数据上下文
    /// </summary>
    public interface IDataContext : IUnitOfWork, IDisposable
    {
        /// <summary>
        /// 获取跟踪的对象集合
        /// </summary>
        ICollection TrackingObjects { get; }

        /// <summary>
        /// 判断此 <paramref name="entity"/> 是否存在于当前上下文中
        /// </summary>
        bool Contains(object entity);
        /// <summary>
        /// 从当前上下文中分离此 <paramref name="entity"/>
        /// </summary>
        void Detach(object entity);
        /// <summary>
        /// 从当前上下文中附加此 <paramref name="entity"/>
        /// </summary>
        void Attach(object entity);
        /// <summary>
        /// 从数据库中刷新(触发sql-select)
        /// </summary>
        void Refresh(object entity);
        /// <summary>
        /// 自动保存或更新
        /// </summary>
        void SaveOrUpdate(object entity);
        /// <summary>
        /// 保存 <paramref name="entity"/> 到数据库(提交时会触发sql-insert)
        /// </summary>
        void Save(object entity);
        /// <summary>
        /// 更新 <paramref name="entity"/> 到数据库(提交时会触发sql-update)
        /// </summary>
        void Update(object entity);
        /// <summary>
        /// 从数据库中删除 <paramref name="entity"/>(提交时会触发sql-delete)
        /// </summary>
        void Delete(object entity);
        /// <summary>
        /// 获取实例信息
        /// </summary>
        object Find(Type type, object id);
        /// <summary>
        /// 获取实例信息
        /// </summary>
        /// <returns></returns>
        object Find(Type type, params object[] keyValues);
        /// <summary>
        /// 加载数据
        /// </summary>
        void Load(object entity);
        

        ///// <summary>
        ///// 提交所有更改的实体。
        ///// </summary>
        //void Commit();


        /// <summary>
        /// 获取对数据类型已知的特定数据源的查询进行计算的功能。
        /// </summary>
        IQueryable<TEntity> CreateQuery<TEntity>() where TEntity : class;

        /// <summary>
        /// 获取当前的数据连接
        /// </summary>
        IDbConnection DbConnection { get; }

        /// <summary>
        /// 当数据提交后执行
        /// </summary>
        event EventHandler DataCommitted;
    }


    //public class MemoryContext : DisposableObject, IDataContext
    //{
    //    private static Dictionary<Type, ISet<object>> entityCollection = new Dictionary<Type, ISet<object>>();



    //    public ICollection TrackingObjects
    //    {
    //        get { return localNewCollection.Union(localModifiedCollection).Union(localDeletedCollection).ToArray(); }
    //    }

    //    private readonly List<object> localNewCollection = new List<object>();
    //    private readonly List<object> localModifiedCollection = new List<object>();
    //    private readonly List<object> localDeletedCollection = new List<object>();
    //    public void Commit()
    //    {
    //        foreach (var newObj in localNewCollection) {
    //            Add(newObj);
    //        }
    //        foreach (var modifiedObj in localModifiedCollection) {
    //            Remove(modifiedObj);
    //            Add(modifiedObj);
    //        }
    //        foreach (var delObj in localDeletedCollection) {
    //            Remove(delObj);
    //        }

    //        localNewCollection.Clear();
    //        localModifiedCollection.Clear();
    //        localDeletedCollection.Clear();
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        this.Commit();
    //    }

    //    private void Add(object entity)
    //    {
    //        var entityType = entity.GetType();

    //        ISet<object> entities;
    //        if (!entityCollection.TryGetValue(entityType, out entities)) {
    //            entities = new HashSet<object>();
    //            entityCollection.Add(entityType, entities);
    //        }

    //        if (!entities.Contains(entity)) {
    //            entities.Add(entity);
    //            entityCollection[entityType] = entities;
    //        }
    //    }
    //    private void Remove(object entity)
    //    {
    //        var entityType = entity.GetType();

    //        ISet<object> entities;

    //        if (entityCollection.TryGetValue(entityType, out entities)) {

    //            entities.Remove(entity);
    //            //var index = entities.IndexOf(entity);

    //            //if (index != -1) {
    //            //    entities.RemoveAt(index);
    //            //    entityCollection[entityType] = entities;
    //            //}
    //        }
    //    }

    //    public bool Contains(object entity)
    //    {
    //        return localNewCollection.Any(item => entity.Equals(item))
    //             || localModifiedCollection.Any(item => entity.Equals(item))
    //             || entityCollection[entity.GetType()].Any(item => entity.Equals(item));
    //    }

    //    public void Detach(object entity)
    //    {
    //        if (localDeletedCollection.Contains(entity)) {
    //            localDeletedCollection.Remove(entity);
    //        }

    //        if (localModifiedCollection.Contains(entity)) {
    //            localModifiedCollection.Remove(entity);
    //        }

    //        if (localNewCollection.Contains(entity)) {
    //            localNewCollection.Remove(entity);
    //        }
    //    }

    //    public void Attach(object entity)
    //    {
    //        this.Add(entity);
    //    }

    //    public object Get(Type type, object id)
    //    {
    //        Func<object, bool> expression = entity => {
    //            return Activator.CreateInstance(type, id).Equals(entity);
    //        };

    //        if (localNewCollection.Any(expression)) {
    //            return localNewCollection.FirstOrDefault(expression);
    //        }

    //        if (localModifiedCollection.Any(expression)) {
    //            return localNewCollection.FirstOrDefault(expression);
    //        }

    //        ISet<object> entities;
    //        if (entityCollection.TryGetValue(type, out entities)) {
    //            return entities.First(expression);
    //        }

    //        return null;
    //    }

    //    public void Refresh(object entity)
    //    { }

    //    public void Save(object entity)
    //    {
    //        if (localDeletedCollection.Contains(entity))
    //            throw new Exception("The object cannot be registered as a new object since it was marked as deleted.");

    //        if (localModifiedCollection.Contains(entity))
    //            throw new Exception("The object cannot be registered as a new object since it was marked as modified.");

    //        //if (localNewCollection.Contains(entity))
    //        //    throw new AggregateException("The object cannot be registered as a new object, because the same id already exist.");
    //        if (localNewCollection.Contains(entity))
    //            localNewCollection.Remove(entity);

    //        localNewCollection.Add(entity);
    //    }

    //    public void Update(object entity)
    //    {
    //        if (localDeletedCollection.Contains(entity))
    //            throw new Exception("The object cannot be registered as a modified object since it was marked as deleted.");

    //        if (localNewCollection.Contains(entity))
    //            throw new Exception("The object cannot be registered as a modified object since it was marked as created.");

    //        if (localModifiedCollection.Contains(entity))
    //            localModifiedCollection.Remove(entity);

    //        localModifiedCollection.Add(entity);
    //    }

    //    public void Delete(object entity)
    //    {
    //        if (localNewCollection.Contains(entity)) {
    //            if (localNewCollection.Remove(entity))
    //                return;
    //        }
    //        bool removedFromModified = localModifiedCollection.Remove(entity);
    //        if (!localDeletedCollection.Contains(entity)) {
    //            localDeletedCollection.Add(entity);
    //        }
    //    }


    //    public IQueryable<TEntity> CreateQuery<TEntity>() 
    //        where TEntity : class
    //    {
    //        ISet<object> entities;
    //        if (entityCollection.TryGetValue(typeof(TEntity), out entities)) {
    //            IEnumerable<TEntity> data = entities.Cast<TEntity>();

    //            return new EnumerableQuery<TEntity>(data);
    //        }

    //        return new EnumerableQuery<TEntity>(new TEntity[0]);
    //    }

    //    public IDbConnection DbConnection
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public event EventHandler DataCommitted = (sender, args) => { };
    //}
}
